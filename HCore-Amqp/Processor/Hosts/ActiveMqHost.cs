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

        private bool _consumersStopped;

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

                if (_listenersCount > 0)
                {
                    for (var i = 0; i < _listenersCount; i++)
                    {
                        var session = await GetSessionInternallyAsync(AcknowledgementMode.Transactional).ConfigureAwait(false);
                        var destination = await GetDestinationInternallyAsync(session).ConfigureAwait(false);

                        var messageConsumer = await GetMessageConsumerInternallyAsync(session, destination).ConfigureAwait(false);

                        _ = Task.Run(async () => await ProcessMessageAsync(session, messageConsumer).ConfigureAwait(false));
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

            if (_isSession && destination is ActiveMQDestination activeMQDestination)
            {
                activeMQDestination.SetExclusive(true);
            }

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

        private async Task ProcessMessageAsync(ISession session, IMessageConsumer messageConsumer)
        {
            while (true)
            {
                try
                {
                    var message = await messageConsumer.ReceiveAsync().ConfigureAwait(false);

                    if (message == null)
                    {
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
                    if (_consumersStopped)
                    {
                        break;
                    }

                    _logger.LogError($"Critical exception during processing AMQP message: {nmsException}");

                    await InitializeAsync().ConfigureAwait(false);

                    break;
                }
                catch (RescheduleException)
                {
                    // no log, this is "wanted"
                }
                catch (PostponeException)
                {
                    // intentionally locking messages.
                }
                catch (Exception exception)
                {
                    _logger.LogError($"Exception during processing AMQP message, not abandoning it for timeout (this will avoid duplicates): {exception}");
                }

                // "abandon" message and requeue

                await session.RollbackAsync().ConfigureAwait(false);
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
                _consumersStopped |= isShuttingDown;

                if (_messageConsumers.Any())
                {
                    foreach (var messageConsumer in _messageConsumers)
                    {
                        await messageConsumer.CloseAsync().ConfigureAwait(false);

                        messageConsumer.Dispose();
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
