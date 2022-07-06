using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HCore.Amqp.Exceptions;
using HCore.Amqp.Message;
using HCore.Amqp.Messenger.Impl;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using BusMessage = Microsoft.Azure.ServiceBus.Message;

namespace HCore.Amqp.Processor.Hosts
{
    internal class TopicClientHost
    {
        private readonly string _connectionString;
        private readonly int _amqpListenerCount;
        private readonly bool _isTopicSession;

        private readonly string _lowLevelAddress;

        protected string Address { get; set; }

        protected string SubscriptionName { get; set; }

        private TopicClient _topicClient;
        private SubscriptionClient _subscriptionClient;

        private readonly ServiceBusMessengerImpl _messenger;
        
        private readonly CancellationToken CancellationToken;

        private readonly static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        
        private readonly ILogger<ServiceBusMessengerImpl> _logger;

        public TopicClientHost(string connectionString, int amqpListenerCount, string address, string subscriptionName, bool isTopicSession, ServiceBusMessengerImpl messenger, CancellationToken cancellationToken, ILogger<ServiceBusMessengerImpl> logger)
        {
            _connectionString = connectionString;
            _amqpListenerCount = amqpListenerCount;
            _isTopicSession = isTopicSession;

            Address = address;
            SubscriptionName = subscriptionName;
            _lowLevelAddress = Address.ToLower();

            _messenger = messenger;

            _logger = logger;

            CancellationToken = cancellationToken;
        }

        public async Task InitializeAsync()
        {
            await CloseAsync().ConfigureAwait(false);

            _topicClient = new TopicClient(_connectionString, _lowLevelAddress);

            _subscriptionClient = new SubscriptionClient(_connectionString, _lowLevelAddress, SubscriptionName);

            if (_amqpListenerCount > 0)
            {
                if (!_isTopicSession)
                {
                    _subscriptionClient.RegisterMessageHandler(ProcessMessageAsync, new MessageHandlerOptions(ExceptionReceivedHandlerAsync)
                    {
                        MaxConcurrentCalls = _amqpListenerCount,
                        AutoComplete = false,
                        MaxAutoRenewDuration = TimeSpan.FromHours(2)
                    });
                }
                else
                {
                    _subscriptionClient.RegisterSessionHandler(ProcessSessionAsync, new SessionHandlerOptions(ExceptionReceivedHandlerAsync)
                    {
                        MaxConcurrentSessions = _amqpListenerCount,
                        AutoComplete = false,
                        MaxAutoRenewDuration = TimeSpan.FromHours(2),
                        MessageWaitTimeout = TimeSpan.FromSeconds(1)
                    });
                }
            }
        }

        private async Task ProcessMessageAsync(BusMessage message, CancellationToken token)
        {
            SubscriptionClient subscriptionClient = _subscriptionClient;

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

                await subscriptionClient.CompleteAsync(message.SystemProperties.LockToken).ConfigureAwait(false);
            }
            catch (RescheduleException)
            {
                // no log, this is "wanted"

                await subscriptionClient.AbandonAsync(message.SystemProperties.LockToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError($"Exception during processing AMQP message, not abandoning it for timeout (this will avoid duplicates): {exception}");
            }
        }

        private async Task ProcessSessionAsync(IMessageSession session, BusMessage firstMessage, CancellationToken token)
        {
            IList<BusMessage> messages;

            try
            {
                if (!string.Equals(firstMessage.ContentType, "application/json"))
                {
                    throw new Exception($"Invalid content type for AMQP message: {firstMessage.ContentType}");
                }

                // try to receive as many messages as possible

                messages = new List<BusMessage>()
                {
                    firstMessage
                };

                var otherMessages = await session.ReceiveAsync(int.MaxValue, TimeSpan.FromSeconds(1)).ConfigureAwait(false);

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

                return;
            }

            try
            {
                var bodies = messages
                    .Select(message => message.Body != null
                        ? Encoding.UTF8.GetString(message.Body)
                        : null)
                    .ToList();

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
            if (_subscriptionClient != null)
            {
                await _subscriptionClient.CloseAsync().ConfigureAwait(false);

                _subscriptionClient = null;
            }

            if (_topicClient != null)
            {
                await _topicClient.CloseAsync().ConfigureAwait(false);

                _topicClient = null;
            }
        }

        public async Task SendMessageAsync(AMQPMessage messageBody, double? timeOffsetSeconds = null)
        {
            TopicClient topicClient;
            bool error;

            do
            {
                topicClient = _topicClient;
                error = false;

                if (CancellationToken.IsCancellationRequested)
                {
                    throw new Exception("AMQP cancellation is requested, can not send message");
                }

                try
                {
                    if (topicClient == null || topicClient.IsClosedOrClosing)
                    {
                        await InitializeAsync().ConfigureAwait(false);

                        topicClient = _topicClient;
                    }

                    var message = new BusMessage(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageBody)))
                    {
                        ContentType = "application/json",
                        MessageId = Guid.NewGuid().ToString()
                    };

                    if (timeOffsetSeconds == null)
                    {
                        await topicClient.SendAsync(message).ConfigureAwait(false);
                    }
                    else
                    {
                        await topicClient.ScheduleMessageAsync(message, DateTimeOffset.UtcNow.AddSeconds((double)timeOffsetSeconds)).ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    error = true;

                    await _semaphore.WaitAsync().ConfigureAwait(false);

                    try
                    {
                        if (topicClient == _topicClient)
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
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                    }
                }
            } while (error);
        }
    }
}
