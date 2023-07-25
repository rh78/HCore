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
using Azure.Messaging.ServiceBus;

namespace HCore.Amqp.Processor.Hosts
{
    internal class QueueClientHost
    {
        private readonly string _connectionString;
        private readonly int _amqpListenerCount;
        private readonly bool _isSession;

        private readonly string _lowLevelAddress;

        protected string Address { get; set; }

        private ServiceBusClient _serviceBusClient;

        private ServiceBusSender _serviceBusSender;
        private ServiceBusProcessor _serviceBusProcessor;
        private ServiceBusSessionProcessor _serviceBusSessionProcessor;

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

            _serviceBusClient = new ServiceBusClient(_connectionString);

            _serviceBusSender = _serviceBusClient.CreateSender(_lowLevelAddress);

            if (_amqpListenerCount > 0)
            {
                if (!_isSession)
                {
                    _serviceBusProcessor = _serviceBusClient.CreateProcessor(_lowLevelAddress, new ServiceBusProcessorOptions()
                    {
                        MaxConcurrentCalls = _amqpListenerCount,
                        AutoCompleteMessages = false,
                        MaxAutoLockRenewalDuration = TimeSpan.FromHours(2)
                    });

                    _serviceBusProcessor.ProcessMessageAsync += ProcessMessageAsync;
                    _serviceBusProcessor.ProcessErrorAsync += ExceptionReceivedHandlerAsync;

                    await _serviceBusProcessor.StartProcessingAsync().ConfigureAwait(false);
                }
                else
                {
                    _serviceBusSessionProcessor = _serviceBusClient.CreateSessionProcessor(_lowLevelAddress, new ServiceBusSessionProcessorOptions()
                    {
                        MaxConcurrentSessions = _amqpListenerCount,
                        AutoCompleteMessages = false,
                        MaxAutoLockRenewalDuration = TimeSpan.FromHours(2),
                        SessionIdleTimeout = TimeSpan.FromSeconds(1)
                    });

                    _serviceBusSessionProcessor.ProcessMessageAsync += ProcessSessionAsync;
                    _serviceBusSessionProcessor.ProcessErrorAsync += ExceptionReceivedHandlerAsync;

                    await _serviceBusSessionProcessor.StartProcessingAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
        {
            var message = args.Message;

            try
            {
                if (!string.Equals(message.ContentType, "application/json"))
                {
                    throw new Exception($"Invalid content type for AMQP message: {message.ContentType}");
                }

                if (message.Body != null)
                {
                    await _messenger.ProcessMessageAsync(Address, Encoding.UTF8.GetString(message.Body)).ConfigureAwait(false);
                } 
                else
                {
                    await _messenger.ProcessMessageAsync(Address, null).ConfigureAwait(false);
                }

                await args.CompleteMessageAsync(message).ConfigureAwait(false);
            }
            catch (RescheduleException)
            {
                // no log, this is "wanted"

                await args.AbandonMessageAsync(message).ConfigureAwait(false);
            }
            catch (PostponeException)
            {
                // intentionally locking a message. By default, the lock duration is 1 minute
            }
            catch (Exception exception)
            {
                _logger.LogError($"Exception during processing AMQP message, not abandoning it for timeout (this will avoid duplicates): {exception}");
            }
        }

        private async Task ProcessSessionAsync(ProcessSessionMessageEventArgs args)
        {
            IList<ServiceBusReceivedMessage> messages;

            try
            {
                var firstMessage = args.Message;

                if (!string.Equals(firstMessage.ContentType, "application/json"))
                {
                    throw new Exception($"Invalid content type for AMQP message: {firstMessage.ContentType}");
                }

                // try to receive as many messages as possible

                messages = new List<ServiceBusReceivedMessage>()
                {
                    firstMessage
                };

                var receiveActions = args.GetReceiveActions();

                var otherMessages = await receiveActions.ReceiveMessagesAsync(int.MaxValue, TimeSpan.FromSeconds(1)).ConfigureAwait(false);

                if (otherMessages != null && otherMessages.Count > 0)
                {
                    foreach (var otherMessage in otherMessages)
                    {
                        if (!string.Equals(otherMessage.ContentType, "application/json"))
                        {
                            throw new Exception($"Invalid content type for AMQP message: {otherMessage.ContentType}");
                        }

                        messages.Add(otherMessage);
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.LogError($"Exception during processing AMQP message, not abandoning it for timeout (this will avoid duplicates): {exception}");
                
                await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

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
                    await args.CompleteMessageAsync(message).ConfigureAwait(false);
                }
            }
            catch (RescheduleException)
            {
                // no log, this is "wanted"

                foreach (var message in messages)
                {
                    await args.AbandonMessageAsync(message).ConfigureAwait(false);
                }
            }
            catch (PostponeException)
            {
                // intentionally locking messages. The default lock duration no longer works for sessions, so we add a delay now.

                await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError($"Exception during processing AMQP message(s), not abandoning them for timeout (this will avoid duplicates): {exception}");

                await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            }
        }

        private Task ExceptionReceivedHandlerAsync(ProcessErrorEventArgs processErrorEventArgs)
        {
            _logger.LogError($"AMQP message handler encountered an exception: {processErrorEventArgs.Exception}");

            var entityPath = processErrorEventArgs.EntityPath;

            Console.WriteLine("Exception context for troubleshooting:");

            Console.WriteLine($"- Entity Path: {entityPath}");

            return Task.CompletedTask;
        }

        public async Task CloseReceiverAsync()
        {
            if (_serviceBusProcessor != null)
            {
                await _serviceBusProcessor.DisposeAsync().ConfigureAwait(false);

                _serviceBusProcessor = null;
            }

            if (_serviceBusSessionProcessor != null)
            {
                await _serviceBusSessionProcessor.DisposeAsync().ConfigureAwait(false);

                _serviceBusSessionProcessor = null;
            }
        }

        public virtual async Task CloseAsync()
        {
            if (_serviceBusProcessor != null)
            {
                await _serviceBusProcessor.DisposeAsync().ConfigureAwait(false);

                _serviceBusProcessor = null;
            }

            if (_serviceBusSessionProcessor != null)
            {
                await _serviceBusSessionProcessor.DisposeAsync().ConfigureAwait(false);

                _serviceBusSessionProcessor = null;
            }

            if (_serviceBusSender != null)
            {
                await _serviceBusSender.DisposeAsync().ConfigureAwait(false);

                _serviceBusSender = null;
            }

            if (_serviceBusClient != null)
            {
                await _serviceBusClient.DisposeAsync().ConfigureAwait(false);

                _serviceBusClient = null;
            }
        }

        public async Task SendMessageAsync(AMQPMessage messageBody, double? timeOffsetSeconds = null, string sessionId = null)
        {
            ServiceBusSender serviceBusSender;
            bool error;

            do
            {
                serviceBusSender = _serviceBusSender;
                error = false;

                if (CancellationToken.IsCancellationRequested)
                {
                    throw new Exception("AMQP cancellation is requested, can not send message");
                }

                try
                {
                    if (serviceBusSender == null || serviceBusSender.IsClosed)
                    {
                        await InitializeAsync().ConfigureAwait(false);

                        serviceBusSender = _serviceBusSender;
                    }

                    var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageBody)))
                    {
                        ContentType = "application/json",
                        MessageId = Guid.NewGuid().ToString()
                    };

                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        if (!_isSession)
                        {
                            throw new Exception("Service Bus AMQP queue is no session queue");
                        }

                        message.SessionId = sessionId;
                    }

                    if (_isSession && string.IsNullOrEmpty(sessionId))
                    {
                        throw new Exception("Session ID is missing for Service Bus AMQP session queue");
                    }

                    if (timeOffsetSeconds == null)
                    {
                        await serviceBusSender.SendMessageAsync(message).ConfigureAwait(false);
                    }
                    else
                    {
                        await serviceBusSender.ScheduleMessageAsync(message, DateTimeOffset.UtcNow.AddSeconds((double)timeOffsetSeconds)).ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    error = true;

                    await _semaphore.WaitAsync().ConfigureAwait(false);

                    try 
                    { 
                        if (serviceBusSender == _serviceBusSender)
                        {
                            // nobody else handled this before

                            if (!CancellationToken.IsCancellationRequested)
                            {
                                _logger.LogError($"AMQP exception in sender link for address {Address}: {e}");
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
                        _semaphore.Release();
                    }

                    if (!CancellationToken.IsCancellationRequested)
                        await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                }
            } while (error);
        }        
    }
}
