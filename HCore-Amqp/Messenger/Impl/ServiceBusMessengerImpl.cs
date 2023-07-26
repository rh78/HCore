using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using HCore.Amqp.Message;
using HCore.Amqp.Processor;
using HCore.Amqp.Processor.Hosts;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HCore.Amqp.Messenger.Impl
{
    internal class ServiceBusMessengerImpl : IAMQPMessenger
    {
        private readonly Dictionary<string, QueueClientHost> _queueClientHosts = new Dictionary<string, QueueClientHost>();
        private readonly Dictionary<string, TopicClientHost> _topicClientHosts = new Dictionary<string, TopicClientHost>();

        private readonly string _connectionString;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _cancellationToken;

        private readonly IAMQPMessageProcessor _messageProcessor;

        private readonly string[] _addresses;
        private readonly string[] _topicAddresses;

        private readonly int[] _addressListenerCounts;
        private readonly int[] _topicAddressListenerCounts;

        private readonly bool[] _isSessions;
        private readonly bool[] _isTopicSessions;

        private readonly ILogger<ServiceBusMessengerImpl> _logger;

        private int _openTasks = 0;

        public ServiceBusMessengerImpl(string connectionString, string[] addresses, int[] addressListenerCount, bool[] isSessions, IAMQPMessageProcessor messageProcessor, ILogger<ServiceBusMessengerImpl> logger)
            : this(connectionString, addresses, topicAddresses: new string[0], addressListenerCount, topicAddressListenerCount: new int[0], isSessions, new bool[0], messageProcessor, logger)
        {
        }

        public ServiceBusMessengerImpl(
            string connectionString,
            string[] addresses,
            string[] topicAddresses,
            int[] addressListenerCount,
            int[] topicAddressListenerCount,
            bool[] isSessions,
            bool[] isTopicSessions,
            IAMQPMessageProcessor messageProcessor,
            ILogger<ServiceBusMessengerImpl> logger)
        {
            _connectionString = connectionString;

            _addresses = addresses;
            _topicAddresses = topicAddresses;
            _addressListenerCounts = addressListenerCount;
            _topicAddressListenerCounts = topicAddressListenerCount;
            _isSessions = isSessions;
            _isTopicSessions = isTopicSessions;

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            _messageProcessor = messageProcessor;

            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            Console.WriteLine("Initializing AMQP...");

            var serviceBusAdministrationClient = new ServiceBusAdministrationClient(_connectionString);

            for (int i = 0; i < _addresses.Length; i++)
            {
                var address = _addresses[i];
                var listenerCount = _addressListenerCounts[i];
                var isSession = _isSessions[i];

                if (await serviceBusAdministrationClient.TopicExistsAsync(address).ConfigureAwait(false))
                {
                    throw new Exception($"Address {address} already exists as a topic");
                }

                if (!await serviceBusAdministrationClient.QueueExistsAsync(address).ConfigureAwait(false))
                {
                    Console.WriteLine($"Creating AMQP queue {address}...");

                    await serviceBusAdministrationClient.CreateQueueAsync(new CreateQueueOptions(address)
                    {
                        LockDuration = TimeSpan.FromMinutes(1),
                        MaxDeliveryCount = int.MaxValue,
                        EnablePartitioning = true,
                        MaxSizeInMegabytes = 2048,
                        RequiresSession = isSession
                    }).ConfigureAwait(false);

                    Console.WriteLine($"Created AMQP queue {address}");
                }

                await AddQueueClientAsync(listenerCount, address, isSession).ConfigureAwait(false);
            }

            for (int i = 0; i < _topicAddresses.Length; i++)
            {
                var topicAddress = _topicAddresses[i];
                var listenerCount = _topicAddressListenerCounts[i];
                var isTopicSession = _isTopicSessions[i];

                if (await serviceBusAdministrationClient.QueueExistsAsync(topicAddress).ConfigureAwait(false))
                {
                    throw new Exception($"Address {topicAddress} already exists as a queue");
                }

                if (!await serviceBusAdministrationClient.TopicExistsAsync(topicAddress).ConfigureAwait(false))
                {
                    Console.WriteLine($"Creating AMQP topic {topicAddress}...");

                    await serviceBusAdministrationClient.CreateTopicAsync(new CreateTopicOptions(topicAddress)
                    {
                        DefaultMessageTimeToLive = TimeSpan.FromMinutes(1),
                        EnablePartitioning = true,
                        MaxSizeInMegabytes = 2048
                    }).ConfigureAwait(false);

                    Console.WriteLine($"Created AMQP topic {topicAddress}");
                }

                var subscriptionName = $"subscription_{Environment.MachineName}";

                if (!await serviceBusAdministrationClient.SubscriptionExistsAsync(topicAddress, subscriptionName).ConfigureAwait(false))
                {
                    await serviceBusAdministrationClient.CreateSubscriptionAsync(new CreateSubscriptionOptions(topicAddress, subscriptionName)
                    {
                        RequiresSession = isTopicSession,
                        AutoDeleteOnIdle = TimeSpan.FromDays(1)
                    }).ConfigureAwait(false);
                }

                await AddTopicClientAsync(listenerCount, topicAddress, subscriptionName, isTopicSession).ConfigureAwait(false);
            }

            Console.WriteLine($"AMQP initialized successfully");
        }

        private async Task AddQueueClientAsync(int amqpListenerCount, string address, bool isSession)
        {
            var queueClientHost = new QueueClientHost(_connectionString, amqpListenerCount, address, isSession, this, _cancellationToken, _logger);

            _queueClientHosts.Add(address, queueClientHost);

            await queueClientHost.InitializeAsync().ConfigureAwait(false);
        }

        private async Task AddTopicClientAsync(int amqpListenerCount, string address, string subscriptionName, bool isTopicSession)
        {
            var topicClientHost = new TopicClientHost(_connectionString, amqpListenerCount, address, subscriptionName, isTopicSession, this, _cancellationToken, _logger);

            _topicClientHosts.Add(address, topicClientHost);

            await topicClientHost.InitializeAsync().ConfigureAwait(false);
        }

        public async Task ShutdownReceiversAsync()
        {
            Console.WriteLine("Shutting down AMQP receivers...");

            try
            {
                foreach (QueueClientHost queueClientHost in _queueClientHosts.Values)
                {
                    await queueClientHost.CloseReceiverAsync().ConfigureAwait(false);
                }

                foreach (TopicClientHost topicClientHost in _topicClientHosts.Values)
                {
                    await topicClientHost.CloseReceiverAsync().ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                // ignore all shutdown faults
            }

            Console.WriteLine("AMQP receivers shut down successfully");
        }

        public async Task WaitForTaskCompletionAsync()
        {
            while (_openTasks > 0)
            {
                await Task.Delay(5000).ConfigureAwait(false);
            }
        }

        public async Task ShutdownAsync()
        {
            Console.WriteLine("Shutting down AMQP...");

            try
            {
                _cancellationTokenSource.Cancel();

                foreach (QueueClientHost queueClientHost in _queueClientHosts.Values)
                {
                    await queueClientHost.CloseAsync().ConfigureAwait(false);
                }

                foreach (TopicClientHost topicClientHost in _topicClientHosts.Values)
                {
                    await topicClientHost.CloseAsync().ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                // ignore all shutdown faults
            }

            Console.WriteLine("AMQP shut down successfully");
        }

        public async Task SendMessageAsync(string address, AMQPMessage body, double? timeOffsetSeconds = null, string sessionId = null)
        {
            Interlocked.Increment(ref _openTasks);

            try
            {
                if (_topicClientHosts.TryGetValue(address, out var topicClientHost))
                {
                    await topicClientHost.SendMessageAsync(body, timeOffsetSeconds);

                    return;
                }

                if (!_queueClientHosts.TryGetValue(address, out var queueClientHost))
                {
                    throw new Exception($"Address {address} is not available for AMQP sending");
                }

                await queueClientHost.SendMessageAsync(body, timeOffsetSeconds, sessionId).ConfigureAwait(false);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                Interlocked.Decrement(ref _openTasks);
            }
        }

        public async Task SendMessageTrySynchronousFirstAsync(string address, AMQPMessage body, double? timeOffsetSeconds = null, string sessionId = null)
        {
            Interlocked.Increment(ref _openTasks);

            try
            {
                await ProcessMessageAsync(address, JsonConvert.SerializeObject(body)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (!_cancellationToken.IsCancellationRequested)
                {
                    _logger.LogError($"AMQP exception in sender link for address {address}: {e}");

                    await SendMessageAsync(address, body, timeOffsetSeconds, sessionId).ConfigureAwait(false);
                }
            }
            finally
            {
                Interlocked.Decrement(ref _openTasks);
            }
        }

        public async Task ProcessMessageAsync(string address, string messageBodyJson)
        {
            Interlocked.Increment(ref _openTasks);

            try
            {
                await _messageProcessor.ProcessMessageAsync(address, messageBodyJson).ConfigureAwait(false);
            }
            catch(Exception)
            {
                throw;
            }
            finally
            {
                Interlocked.Decrement(ref _openTasks);
            }
        }

        public async Task ProcessMessagesAsync(string address, List<string> messageBodyJsons)
        {
            Interlocked.Increment(ref _openTasks);

            try
            {
                await _messageProcessor.ProcessMessagesAsync(address, messageBodyJsons).ConfigureAwait(false);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                Interlocked.Decrement(ref _openTasks);
            }
        }

        public async Task<bool?> IsAvailableAsync(CancellationToken cancellationToken)
        {
            try
            {
                var serviceBusAdministrationClient = new ServiceBusAdministrationClient(_connectionString);

                var address = _addresses.FirstOrDefault();

                if (!string.IsNullOrEmpty(address))
                {
                    return await serviceBusAdministrationClient.QueueExistsAsync(address, cancellationToken).ConfigureAwait(false);
                }

                var topicAddress = _topicAddresses.FirstOrDefault();

                if (!string.IsNullOrEmpty(topicAddress))
                {
                    return await serviceBusAdministrationClient.TopicExistsAsync(address, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (ServiceBusException)
            {
                // Nothing to do here
            }
            catch (OperationCanceledException)
            {
                return null;
            }

            return false;
        }
    }
}
