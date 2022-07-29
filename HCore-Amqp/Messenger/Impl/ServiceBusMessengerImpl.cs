using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HCore.Amqp.Message;
using HCore.Amqp.Processor;
using HCore.Amqp.Processor.Hosts;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Hosting;
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

        public ServiceBusMessengerImpl(string connectionString, string[] addresses, int[] addressListenerCount, bool[] isSessions, IHostApplicationLifetime applicationLifetime, IAMQPMessageProcessor messageProcessor, ILogger<ServiceBusMessengerImpl> logger)
            : this(connectionString, addresses, topicAddresses: new string[0], addressListenerCount, topicAddressListenerCount: new int[0], isSessions, new bool[0], applicationLifetime, messageProcessor, logger)
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
            IHostApplicationLifetime applicationLifetime,
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

            applicationLifetime.ApplicationStopping.Register(OnShutdown);
        }

        public async Task InitializeAsync()
        {
            Console.WriteLine("Initializing AMQP...");

            var managementClient = new ManagementClient(_connectionString);

            for (int i = 0; i < _addresses.Length; i++)
            {
                var address = _addresses[i];
                var listenerCount = _addressListenerCounts[i];
                var isSession = _isSessions[i];

                if (await managementClient.TopicExistsAsync(address).ConfigureAwait(false))
                {
                    throw new Exception($"Address {address} already exists as a topic");
                }

                if (!await managementClient.QueueExistsAsync(address).ConfigureAwait(false))
                {
                    Console.WriteLine($"Creating AMQP queue {address}...");

                    await managementClient.CreateQueueAsync(new QueueDescription(address)
                    {
                        LockDuration = TimeSpan.FromMinutes(1),
                        MaxDeliveryCount = int.MaxValue,
                        EnablePartitioning = true,
                        MaxSizeInMB = 2048,
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

                if (await managementClient.QueueExistsAsync(topicAddress).ConfigureAwait(false))
                {
                    throw new Exception($"Address {topicAddress} already exists as a queue");
                }

                if (!await managementClient.TopicExistsAsync(topicAddress).ConfigureAwait(false))
                {
                    Console.WriteLine($"Creating AMQP topic {topicAddress}...");

                    await managementClient.CreateTopicAsync(new TopicDescription(topicAddress)
                    {
                        DefaultMessageTimeToLive = TimeSpan.FromMinutes(1),
                        EnablePartitioning = true,
                        MaxSizeInMB = 2048
                    }).ConfigureAwait(false);

                    Console.WriteLine($"Created AMQP topic {topicAddress}");
                }

                var subscriptionName = $"subscription_{Environment.MachineName}";

                if (!await managementClient.SubscriptionExistsAsync(topicAddress, subscriptionName).ConfigureAwait(false))
                {
                    await managementClient.CreateSubscriptionAsync(new SubscriptionDescription(topicAddress, subscriptionName)
                    {
                        RequiresSession = isTopicSession,
                        AutoDeleteOnIdle = TimeSpan.FromDays(1)
                    }).ConfigureAwait(false);
                }

                await AddTopicClientAsync(listenerCount, topicAddress, subscriptionName, isTopicSession).ConfigureAwait(false);
            }

            await managementClient.CloseAsync().ConfigureAwait(false);

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

        private void OnShutdown()
        {
            Console.WriteLine("Shutting down AMQP...");

            try
            {
                _cancellationTokenSource.Cancel();

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                foreach (QueueClientHost queueClientHost in _queueClientHosts.Values)
                    queueClientHost.CloseAsync().Wait();

                foreach (TopicClientHost topicClientHost in _topicClientHosts.Values)
                    topicClientHost.CloseAsync().Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
            }
            catch (Exception)
            {
                // ignore all shutdown faults
            }

            Console.WriteLine("AMQP shut down successfully");
        }

        public async Task SendMessageAsync(string address, AMQPMessage body, double? timeOffsetSeconds = null, string sessionId = null)
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

        public async Task SendMessageTrySynchronousFirstAsync(string address, AMQPMessage body, double? timeOffsetSeconds = null, string sessionId = null)
        {
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
        }

        public async Task ProcessMessageAsync(string address, string messageBodyJson)
        {
            await _messageProcessor.ProcessMessageAsync(address, messageBodyJson).ConfigureAwait(false);
        }

        public async Task ProcessMessagesAsync(string address, List<string> messageBodyJsons)
        {
            await _messageProcessor.ProcessMessagesAsync(address, messageBodyJsons).ConfigureAwait(false);
        }

        public async Task<bool?> IsAvailableAsync(CancellationToken cancellationToken)
        {
            try
            {
                var managementClient = new ManagementClient(_connectionString);

                var address = _addresses.FirstOrDefault();

                if (!string.IsNullOrEmpty(address))
                {
                    return await managementClient.QueueExistsAsync(address, cancellationToken).ConfigureAwait(false);
                }

                var topicAddress = _topicAddresses.FirstOrDefault();

                if (!string.IsNullOrEmpty(topicAddress))
                {
                    return await managementClient.TopicExistsAsync(address, cancellationToken).ConfigureAwait(false);
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
