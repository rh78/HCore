using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.ActiveMQ.Commands;
using HCore.Amqp.Exceptions;
using HCore.Amqp.Message;
using HCore.Amqp.Messenger.Impl;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NmsMessage = Apache.NMS.ActiveMQ.Commands.Message;

namespace HCore.Amqp.Processor.Hosts
{
    internal abstract class ActiveMqHost
    {
        private const string _scheduledDelayKey = "AMQ_SCHEDULED_DELAY";

        // the same as the lock duration of the Service Bus queues and the default session lock on postpone

        private static readonly TimeSpan _errorHoldTimeSpan = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan _postponeHoldTimeSpan = TimeSpan.FromSeconds(10);

        private static readonly TimeSpan _recreateListenerRetryTimeSpan = TimeSpan.FromSeconds(5);

        private readonly SemaphoreSlim _initSemaphoreSlim = new(1, 1);
        private readonly SemaphoreSlim _closeSemaphoreSlim = new(1, 1);

        private readonly SemaphoreSlim _producerSemaphoreSlim = new(1, 1);

        private readonly int _listenersCount;

        private readonly string _address;

        private readonly bool _isSession;

        private readonly ActiveMqMessengerImpl _activeMqMessengerImpl;

        private readonly CancellationToken _producerCancellationToken;

        private readonly ILogger<ActiveMqMessengerImpl> _logger;

        private readonly ICollection<ISession> _sessions = [];
        private readonly ICollection<IDestination> _destinations = [];
        private readonly ICollection<IMessageConsumer> _messageConsumers = [];

        private IConnection _connection;
        private ISession _producerSession;
        private IMessageProducer _messageProducer;

        private volatile bool _consumersStopped;

        private readonly CancellationTokenSource _consumersCancellationTokenSource = new();

        internal ActiveMqHost(int listenersCount, string address, bool isSession, ActiveMqMessengerImpl activeMqMessengerImpl, CancellationToken cancellationToken, ILogger<ActiveMqMessengerImpl> logger)
        {
            _listenersCount = listenersCount;

            _address = address;

            _isSession = isSession;

            _activeMqMessengerImpl = activeMqMessengerImpl;

            _producerCancellationToken = cancellationToken;

            _logger = logger;
        }

        internal async Task InitializeAsync()
        {
            await _initSemaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                await CloseAsync().ConfigureAwait(false);

                _connection = await _activeMqMessengerImpl.GetConnectionAsync().ConfigureAwait(false);

                await _connection.StartAsync().ConfigureAwait(false);

                _producerSession = await GetSessionInternallyAsync(AcknowledgementMode.AutoAcknowledge).ConfigureAwait(false);

                var producerDestination = await GetDestinationInternallyAsync(_producerSession).ConfigureAwait(false);

                _messageProducer = await _producerSession.CreateProducerAsync(producerDestination).ConfigureAwait(false);
                _messageProducer.DeliveryMode = MsgDeliveryMode.Persistent;

                // a send during shutdown can re-initialize the connection; it must not start consuming again

                if (_listenersCount > 0 && !_consumersStopped)
                {
                    var connection = _connection;

                    for (var i = 0; i < _listenersCount; i++)
                    {
                        var session = await GetSessionInternallyAsync(AcknowledgementMode.Transactional).ConfigureAwait(false);
                        var destination = await GetDestinationInternallyAsync(session).ConfigureAwait(false);

                        var messageConsumer = await GetMessageConsumerInternallyAsync(session, destination).ConfigureAwait(false);

                        _ = Task.Run(async () => await RunListenerAsync(connection, session, messageConsumer).ConfigureAwait(false));
                    }
                }
            }
            finally
            {
                _initSemaphoreSlim.Release();
            }
        }

        private async Task<ISession> GetSessionInternallyAsync(AcknowledgementMode acknowledgementMode)
        {
            var session = await _connection.CreateSessionAsync(acknowledgementMode).ConfigureAwait(false);

            _sessions.Add(session);

            return session;
        }

        private async Task<IDestination> GetDestinationInternallyAsync(ISession session)
        {
            var destination = await GetDestinationAsync(session, _address).ConfigureAwait(false);

            _destinations.Add(destination);

            return destination;
        }

        protected virtual async Task<IDestination> GetDestinationAsync(ISession session, string address)
        {
            var destination = await session.GetQueueAsync(address).ConfigureAwait(false);

            return destination;
        }

        private async Task<IMessageConsumer> GetMessageConsumerInternallyAsync(ISession session, IDestination destination)
        {
            var messageConsumer = await GetMessageConsumerAsync(session, destination).ConfigureAwait(false);

            _messageConsumers.Add(messageConsumer);

            return messageConsumer;
        }

        protected virtual async Task<IMessageConsumer> GetMessageConsumerAsync(ISession session, IDestination destination)
        {
            var messageConsumer = await session.CreateConsumerAsync(destination).ConfigureAwait(false);

            return messageConsumer;
        }

        // A message that cannot be processed now is handed back by closing this listener's consumer: the broker keeps it at
        // the head of its message group, releases every group the consumer owned, and dispatches them to any free consumer.
        // Holding the message before that keeps a failing group from being retried in a tight loop.

        private async Task RunListenerAsync(IConnection connection, ISession session, IMessageConsumer messageConsumer)
        {
            while (true)
            {
                var holdTimeSpan = await ProcessMessagesAsync(connection, session, messageConsumer).ConfigureAwait(false);

                if (holdTimeSpan == null)
                {
                    return;
                }

                // on shutdown the held message is rolled back when the session is closed

                if (!await TryDelayAsync(holdTimeSpan.Value).ConfigureAwait(false))
                {
                    return;
                }

                if (!HandsBackFailedMessages && await TryRollbackAsync(session).ConfigureAwait(false))
                {
                    continue;
                }

                await ReleaseListenerAsync(session, messageConsumer).ConfigureAwait(false);

                (session, messageConsumer) = await RecreateListenerAsync(connection).ConfigureAwait(false);

                if (session == null)
                {
                    return;
                }
            }
        }

        // a closed consumer of a non-durable topic subscription loses its unacknowledged message,
        // so topics redeliver a failed message locally instead of handing it back to the broker

        protected virtual bool HandsBackFailedMessages => true;

        private async Task<bool> TryRollbackAsync(ISession session)
        {
            try
            {
                await session.RollbackAsync().ConfigureAwait(false);

                return true;
            }
            catch (Exception exception)
            {
                _logger.LogWarning($"AMQP rollback for address {_address} failed, recreating the listener: {exception.Message}");

                return false;
            }
        }

        private async Task<bool> TryDelayAsync(TimeSpan timeSpan)
        {
            try
            {
                await Task.Delay(timeSpan, _consumersCancellationTokenSource.Token).ConfigureAwait(false);

                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        // after a shutdown, or a reconnect where InitializeAsync creates new listeners, the listeners of the old connection end

        private bool IsListenerObsolete(IConnection connection)
        {
            return _consumersStopped || _connection != connection;
        }

        private async Task ReleaseListenerAsync(ISession session, IMessageConsumer messageConsumer)
        {
            await _closeSemaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                _messageConsumers.Remove(messageConsumer);
                _sessions.Remove(session);
            }
            finally
            {
                _closeSemaphoreSlim.Release();
            }

            await CloseListenerAsync(session, messageConsumer).ConfigureAwait(false);
        }

        // each step runs even if the previous one failed: a consumer left open would keep its message and its message groups

        private async Task CloseListenerAsync(ISession session, IMessageConsumer messageConsumer)
        {
            await TryCloseStepAsync("roll back", () => session.RollbackAsync()).ConfigureAwait(false);

            if (messageConsumer != null)
            {
                await TryCloseStepAsync("close the consumer of", () => messageConsumer.CloseAsync()).ConfigureAwait(false);

                await TryCloseStepAsync("dispose the consumer of", () =>
                {
                    messageConsumer.Dispose();

                    return Task.CompletedTask;
                }).ConfigureAwait(false);
            }

            await TryCloseStepAsync("close the session of", () => session.CloseAsync()).ConfigureAwait(false);

            await TryCloseStepAsync("dispose the session of", () =>
            {
                session.Dispose();

                return Task.CompletedTask;
            }).ConfigureAwait(false);
        }

        // the connection may already be broken; the broker then releases the consumer when the connection ends

        private async Task TryCloseStepAsync(string step, Func<Task> closeStep)
        {
            try
            {
                await closeStep().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogWarning($"AMQP listener could not {step} address {_address}: {exception.Message}");
            }
        }

        // the broker calls run outside the locks, so a broker that is unreachable cannot block shutdown or sending

        private async Task<(ISession, IMessageConsumer)> RecreateListenerAsync(IConnection connection)
        {
            while (!IsListenerObsolete(connection))
            {
                ISession session = null;
                IMessageConsumer messageConsumer = null;

                try
                {
                    session = await connection.CreateSessionAsync(AcknowledgementMode.Transactional).ConfigureAwait(false);

                    var destination = await GetDestinationAsync(session, _address).ConfigureAwait(false);

                    messageConsumer = await GetMessageConsumerAsync(session, destination).ConfigureAwait(false);

                    if (await TryRegisterListenerAsync(connection, session, messageConsumer).ConfigureAwait(false))
                    {
                        return (session, messageConsumer);
                    }

                    await CloseListenerAsync(session, messageConsumer).ConfigureAwait(false);

                    return (null, null);
                }
                catch (Exception exception)
                {
                    if (session != null)
                    {
                        await CloseListenerAsync(session, messageConsumer).ConfigureAwait(false);
                    }

                    if (IsListenerObsolete(connection))
                    {
                        return (null, null);
                    }

                    _logger.LogError($"AMQP listener for address {_address} could not be recreated, retrying: {exception}");
                }

                if (!await TryDelayAsync(_recreateListenerRetryTimeSpan).ConfigureAwait(false))
                {
                    return (null, null);
                }
            }

            return (null, null);
        }

        // registered under the close lock, so a shutdown or reconnect either sees the new listener or the listener sees it

        private async Task<bool> TryRegisterListenerAsync(IConnection connection, ISession session, IMessageConsumer messageConsumer)
        {
            await _closeSemaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                if (IsListenerObsolete(connection))
                {
                    return false;
                }

                _sessions.Add(session);
                _messageConsumers.Add(messageConsumer);

                return true;
            }
            finally
            {
                _closeSemaphoreSlim.Release();
            }
        }

        // returns how long to hold the current message before handing it back, or null when the listener must stop

        private async Task<TimeSpan?> ProcessMessagesAsync(IConnection connection, ISession session, IMessageConsumer messageConsumer)
        {
            while (true)
            {
                try
                {
                    var message = await messageConsumer.ReceiveAsync().ConfigureAwait(false);

                    if (message == null)
                    {
                        if (IsListenerObsolete(connection))
                        {
                            return null;
                        }

                        continue;
                    }

                    var sessionId = message is NmsMessage nmsMessage
                        ? nmsMessage.GroupID
                        : null;

                    if (message is ITextMessage textMessage)
                    {
                        var body = textMessage.Text;

                        await _activeMqMessengerImpl.ProcessMessageAsync(_address, body, sessionId).ConfigureAwait(false);
                    }
                    else if (message is IMapMessage mapMessage)
                    {
                        if (mapMessage.Body == null)
                        {
                            throw new Exception($"Missing multi body for AMQP message: ({JsonConvert.SerializeObject(message)})");
                        }

                        foreach (var key in mapMessage.Body.Keys)
                        {
                            var body = mapMessage.Body.GetString(key as string);

                            await _activeMqMessengerImpl.ProcessMessageAsync(_address, body, sessionId).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }

                    await session.CommitAsync().ConfigureAwait(false);

                    continue;
                }
                catch (NMSException nmsException)
                {
                    if (IsListenerObsolete(connection))
                    {
                        return null;
                    }

                    _logger.LogError($"NMS exception during processing AMQP message: {nmsException}");

                    return _errorHoldTimeSpan;
                }
                catch (RescheduleException)
                {
                    // no log, this is "wanted"

                    return TimeSpan.Zero;
                }
                catch (PostponeException postponeException)
                {
                    // intentionally holding the message, as long as the Service Bus implementation locks its session

                    return postponeException.LockSessionTimeSpan ?? _postponeHoldTimeSpan;
                }
                catch (Exception exception)
                {
                    _logger.LogError($"Exception during processing AMQP message, holding it before handing it back: {exception}");

                    return _errorHoldTimeSpan;
                }
            }
        }

        internal async Task SendMessageAsync(AMQPMessage body, double? timeOffsetSeconds, string sessionId = null)
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                if (!_isSession)
                {
                    throw new Exception("Active MQ queue is no session queue");
                }
            }
            else if (_isSession)
            {
                throw new Exception("Session ID is missing for Active MQ session queue");
            }

            bool error;

            do
            {
                error = false;

                var session = _producerSession;
                var messageProducer = _messageProducer;

                if (_producerCancellationToken.IsCancellationRequested)
                {
                    throw new Exception("AMQP cancellation is requested, can not send message");
                }

                try
                {
                    if (session == null || messageProducer == null)
                    {
                        await InitializeAsync().ConfigureAwait(false);

                        messageProducer = _messageProducer;
                    }

                    var textMessage = await session.CreateTextMessageAsync(JsonConvert.SerializeObject(body)).ConfigureAwait(false);

                    if (timeOffsetSeconds.HasValue)
                    {
                        textMessage.Properties.SetLong(_scheduledDelayKey, (long)TimeSpan.FromSeconds(timeOffsetSeconds.Value).TotalMilliseconds);
                    }

                    if (!string.IsNullOrEmpty(sessionId) && textMessage is NmsMessage nmsMessage)
                    {
                        nmsMessage.GroupID = sessionId;
                    }

                    await messageProducer.SendAsync(textMessage).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    error = true;

                    await _producerSemaphoreSlim.WaitAsync().ConfigureAwait(false);

                    try
                    {
                        if (messageProducer == _messageProducer)
                        {
                            // nobody else handled this before

                            if (!_producerCancellationToken.IsCancellationRequested)
                            {
                                _logger.LogError($"AMQP exception in sender link for address {_address}: {e}");
                            }

                            await CloseAsync().ConfigureAwait(false);
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        _producerSemaphoreSlim.Release();
                    }

                    if (!_producerCancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                    }
                }
            } while (error);
        }

        internal async Task SendMessagesAsync<T>(ICollection<T> bodies, double? timeOffsetSeconds = null, string sessionId = null) where T : AMQPMessage
        {
            if (timeOffsetSeconds.HasValue)
            {
                throw new NotImplementedException();
            }

            if (_isSession || !string.IsNullOrEmpty(sessionId))
            {
                throw new NotImplementedException();
            }

            bool error;

            do
            {
                error = false;

                var session = _producerSession;
                var messageProducer = _messageProducer;

                if (_producerCancellationToken.IsCancellationRequested)
                {
                    throw new Exception("AMQP cancellation is requested, can not send message");
                }

                try
                {
                    if (session == null || messageProducer == null)
                    {
                        await InitializeAsync().ConfigureAwait(false);

                        messageProducer = _messageProducer;
                    }

                    var mapMessage = await session.CreateMapMessageAsync().ConfigureAwait(false);

                    for (var i = 0; i < bodies.Count; i++)
                    {
                        var body = bodies.ElementAt(i);

                        mapMessage.Body.SetString($"{i}", JsonConvert.SerializeObject(body));
                    }

                    if (timeOffsetSeconds.HasValue)
                    {
                        mapMessage.Properties.SetLong(_scheduledDelayKey, (long)TimeSpan.FromSeconds(timeOffsetSeconds.Value).TotalMilliseconds);
                    }

                    if (!string.IsNullOrEmpty(sessionId) && mapMessage is NmsMessage nmsMessage)
                    {
                        nmsMessage.GroupID = sessionId;
                    }

                    await messageProducer.SendAsync(mapMessage).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    error = true;

                    await _producerSemaphoreSlim.WaitAsync().ConfigureAwait(false);

                    try
                    {
                        if (messageProducer == _messageProducer)
                        {
                            // nobody else handled this before

                            if (!_producerCancellationToken.IsCancellationRequested)
                            {
                                _logger.LogError($"AMQP exception in sender link for address {_address}: {e}");
                            }

                            await CloseAsync().ConfigureAwait(false);
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        _producerSemaphoreSlim.Release();
                    }

                    if (!_producerCancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                    }
                }
            } while (error);
        }

        internal async Task CloseConsumersAsync(bool isShuttingDown)
        {
            await _closeSemaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                if (isShuttingDown)
                {
                    _consumersStopped = true;

                    await _consumersCancellationTokenSource.CancelAsync().ConfigureAwait(false);
                }

                if (_messageConsumers.Any())
                {
                    foreach (var messageConsumer in _messageConsumers)
                    {
                        await TryCloseStepAsync("close a consumer of", () => messageConsumer.CloseAsync()).ConfigureAwait(false);

                        await TryCloseStepAsync("dispose a consumer of", () =>
                        {
                            messageConsumer.Dispose();

                            return Task.CompletedTask;
                        }).ConfigureAwait(false);
                    }

                    _messageConsumers.Clear();
                }
            }
            finally
            {
                _closeSemaphoreSlim.Release();
            }
        }

        internal async Task CloseAsync()
        {
            await CloseConsumersAsync(isShuttingDown: false).ConfigureAwait(false);

            await _closeSemaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                if (_messageProducer != null)
                {
                    await _messageProducer.CloseAsync().ConfigureAwait(false);

                    _messageProducer.Dispose();
                    _messageProducer = null;
                }

                if (_destinations.Any())
                {
                    foreach (var destination in _destinations)
                    {
                        destination.Dispose();
                    }

                    _destinations.Clear();
                }

                if (_sessions.Any())
                {
                    foreach (var session in _sessions)
                    {
                        await session.CloseAsync().ConfigureAwait(false);

                        session.Dispose();
                    }

                    _sessions.Clear();

                    _producerSession = null;
                }

                if (_connection != null)
                {
                    await _connection.CloseAsync().ConfigureAwait(false);

                    _connection.Dispose();
                    _connection = null;
                }
            }
            finally
            {
                _closeSemaphoreSlim.Release();
            }
        }
    }
}
