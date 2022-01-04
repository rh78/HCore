using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HCore.Amqp.Message;
using HCore.Amqp.Messenger.Impl;
using HCore.Amqp.Exceptions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace HCore.Amqp.Processor.Hosts
{
    internal class QueueClientHost
    {
        private readonly string _connectionString;
        private readonly int _amqpListenerCount;
        private readonly bool _isSession;

        private readonly string _lowLevelAddress;

        protected string Address { get; set; }

        private QueueClient _queueClient;

        private readonly ServiceBusMessengerImpl _messenger;

        protected readonly CancellationToken CancellationToken;

        private readonly static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private readonly ILogger<ServiceBusMessengerImpl> _logger;

        public QueueClientHost(string connectionString, int amqpListenerCount, string address, bool isSession, ServiceBusMessengerImpl messenger, CancellationToken cancellationToken, ILogger<ServiceBusMessengerImpl> logger)
        {
            _connectionString = connectionString;
            _amqpListenerCount = amqpListenerCount;
            _isSession = isSession;

            Address = address;
            _lowLevelAddress = Address.ToLower();

            _messenger = messenger;

            _logger = logger;

            CancellationToken = cancellationToken;
        }

        public async Task InitializeAsync()
        {
            await CloseAsync().ConfigureAwait(false);

            _queueClient = new QueueClient(_connectionString, _lowLevelAddress);

            if (_amqpListenerCount > 0)
            {
                if (!_isSession)
                {
                    _queueClient.RegisterMessageHandler(ProcessMessageAsync, new MessageHandlerOptions(ExceptionReceivedHandlerAsync)
                    {
                        MaxConcurrentCalls = _amqpListenerCount,
                        AutoComplete = false,
                        MaxAutoRenewDuration = TimeSpan.FromHours(2)
                    });
                }
                else
                {
                    _queueClient.RegisterSessionHandler(ProcessSessionAsync, new SessionHandlerOptions(ExceptionReceivedHandlerAsync)
                    {
                        MaxConcurrentSessions = _amqpListenerCount,
                        AutoComplete = false,
                        MaxAutoRenewDuration = TimeSpan.FromHours(2),
                        MessageWaitTimeout = TimeSpan.FromSeconds(1)
                    });
                }
            }
        }

        private async Task ProcessMessageAsync(Microsoft.Azure.ServiceBus.Message message, CancellationToken token)
        {
            QueueClient queueClient = _queueClient;

            try
            {
                if (!string.Equals(message.ContentType, "application/json"))
                    throw new Exception($"Invalid content type for AMQP message: {message.ContentType}");

                if (message.Body != null)
                {
                    await _messenger.ProcessMessageAsync(Address, Encoding.UTF8.GetString(message.Body)).ConfigureAwait(false);
                } 
                else
                {
                    await _messenger.ProcessMessageAsync(Address, null).ConfigureAwait(false);
                }

                await queueClient.CompleteAsync(message.SystemProperties.LockToken).ConfigureAwait(false);
            }
            catch (RescheduleException)
            {
                // no log, this is "wanted"

                await queueClient.AbandonAsync(message.SystemProperties.LockToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError($"Exception during processing AMQP message, not abandoning it for timeout (this will avoid duplicates): {exception}");
            }
        }

        private async Task ProcessSessionAsync(IMessageSession session, Microsoft.Azure.ServiceBus.Message firstMessage, CancellationToken token)
        {
            IList<Microsoft.Azure.ServiceBus.Message> messages;

            try
            {
                if (!string.Equals(firstMessage.ContentType, "application/json"))
                    throw new Exception($"Invalid content type for AMQP message: {firstMessage.ContentType}");

                // try to receive as many messages as possible

                messages = new List<Microsoft.Azure.ServiceBus.Message>()
                {
                    firstMessage
                };

                var otherMessages = await session.ReceiveAsync(int.MaxValue, TimeSpan.FromSeconds(1)).ConfigureAwait(false);

                if (otherMessages != null && otherMessages.Count > 0)
                {
                    foreach (var otherMessage in otherMessages)
                    {
                        if (!string.Equals(otherMessage.ContentType, "application/json"))
                            throw new Exception($"Invalid content type for AMQP message: {otherMessage.ContentType}");

                        messages.Add(otherMessage);
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.LogError($"Exception during processing AMQP message, not abandoning it for timeout (this will avoid duplicates): {exception}");

                return;
            }

            try
            {
                var bodies = messages.Select(message =>
                {
                    if (message.Body != null)
                    {
                        return Encoding.UTF8.GetString(message.Body);
                    }
                    else
                    {
                        return null;
                    }
                }).ToList();

                await _messenger.ProcessMessagesAsync(Address, bodies).ConfigureAwait(false);

                foreach (var message in messages)
                {
                    await session.CompleteAsync(message.SystemProperties.LockToken).ConfigureAwait(false);
                }
            }
            catch (RescheduleException)
            {
                // no log, this is "wanted"

                foreach (var message in messages)
                {
                    await session.AbandonAsync(message.SystemProperties.LockToken).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError($"Exception during processing AMQP message(s), not abandoning them for timeout (this will avoid duplicates): {exception}");
            }
        }

        private Task ExceptionReceivedHandlerAsync(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            _logger.LogError($"AMQP message handler encountered an exception: {exceptionReceivedEventArgs.Exception}");

            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;

            Console.WriteLine("Exception context for troubleshooting:");

            Console.WriteLine($"- Endpoint: {context.Endpoint}");
            Console.WriteLine($"- Entity Path: {context.EntityPath}");
            Console.WriteLine($"- Executing Action: {context.Action}");

            return Task.CompletedTask;
        }

        public virtual async Task CloseAsync()
        {
            if (_queueClient != null)
            {
                await _queueClient.CloseAsync().ConfigureAwait(false);

                _queueClient = null;
            }
        }

        public async Task SendMessageAsync(AMQPMessage messageBody, double? timeOffsetSeconds = null, string sessionId = null)
        {
            QueueClient queueClient;
            bool error;

            do
            {
                queueClient = _queueClient;
                error = false;

                if (CancellationToken.IsCancellationRequested)
                    throw new Exception("AMQP cancellation is requested, can not send message");

                try
                {
                    if (queueClient == null || queueClient.IsClosedOrClosing)
                        await InitializeAsync().ConfigureAwait(false);

                    var message = new Microsoft.Azure.ServiceBus.Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageBody)))
                    {
                        ContentType = "application/json",
                        MessageId = Guid.NewGuid().ToString()
                    };

                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        if (!_isSession)
                            throw new Exception("Service Bus AMQP queue is no session queue");

                        message.SessionId = sessionId;
                    }

                    if (_isSession && string.IsNullOrEmpty(sessionId))
                        throw new Exception("Session ID is missing for Service Bus AMQP session queue");

                    if (timeOffsetSeconds == null)
                        await queueClient.SendAsync(message).ConfigureAwait(false);
                    else
                        await queueClient.ScheduleMessageAsync(message, DateTimeOffset.UtcNow.AddSeconds((double)timeOffsetSeconds)).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    error = true;

                    await _semaphore.WaitAsync().ConfigureAwait(false);

                    try { 
                        if (queueClient == _queueClient)
                        {
                            // nobody else handled this before

                            if (!CancellationToken.IsCancellationRequested)
                                _logger.LogError($"AMQP exception in sender link for address {Address}: {e}");

                            await CloseAsync().ConfigureAwait(false);
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        _semaphore.Release();
                    }

                    if (!CancellationToken.IsCancellationRequested)
                        await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                }
            } while (error);
        }        
    }
}
