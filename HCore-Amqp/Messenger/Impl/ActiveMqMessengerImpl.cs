using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.Policies;
using HCore.Amqp.Message;
using HCore.Amqp.Processor;
using HCore.Amqp.Processor.Hosts;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HCore.Amqp.Messenger.Impl
{
    internal class ActiveMqMessengerImpl : IAMQPMessenger
    {
        private readonly Dictionary<string, QueueHost> _queueHostsByAddress = [];
        private readonly Dictionary<string, TopicHost> _topicHostsByAddress = [];

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

        private readonly ILogger<ActiveMqMessengerImpl> _logger;

        private int _openTasks = 0;

        private IConnection _connection;

        public ActiveMqMessengerImpl(string connectionString, string[] addresses, int[] addressListenerCount, bool[] isSessions, IAMQPMessageProcessor messageProcessor, ILogger<ActiveMqMessengerImpl> logger)
            : this(connectionString, addresses, topicAddresses: [], addressListenerCount, topicAddressListenerCount: [], isSessions, [], messageProcessor, logger)
        {
        }

        public ActiveMqMessengerImpl(
            string connectionString,
            string[] addresses,
            string[] topicAddresses,
            int[] addressListenerCount,
            int[] topicAddressListenerCount,
            bool[] isSessions,
            bool[] isTopicSessions,
            IAMQPMessageProcessor messageProcessor,
            ILogger<ActiveMqMessengerImpl> logger)
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

            if (!Uri.TryCreate(_connectionString, UriKind.Absolute, out var connectionStringUri) || string.IsNullOrEmpty(connectionStringUri.UserInfo))
            {
                throw new Exception("AMQP connection string is not valid");
            }

            var userInfoParts = connectionStringUri.UserInfo.Split(":", StringSplitOptions.RemoveEmptyEntries);

            if (userInfoParts.Length != 2)
            {
                throw new Exception("AMQP invalid credentials format");
            }

            // https://activemq.apache.org/components/classic/documentation/failover-transport-reference

            var brokerUri = $"failover:({_connectionString})?maxReconnectAttempts=-1&initialReconnectDelay=1000&maxReconnectDelay=30000";

            // AsyncSend = true - maximum throughput. Uses fire and forget
            // AsyncSend = false - maximum durability. Will wait for broker ack
            // https://activemq.apache.org/components/classic/documentation/async-sends
            // https://activemq.apache.org/components/classic/documentation/consumer-dispatch-async
            // using default behavior (AsyncSend = false; DispatchAsync = true)

            var connectionFactory = new ConnectionFactory(brokerUri)
            {
                AsyncSend = false,
                DispatchAsync = true,
                RedeliveryPolicy = new RedeliveryPolicy
                {
                    InitialRedeliveryDelay = (int)TimeSpan.FromSeconds(60).TotalMilliseconds,
                    MaximumRedeliveries = -1
                }
            };

            var userName = userInfoParts[0];
            var password = userInfoParts[1];

            _connection = await connectionFactory.CreateConnectionAsync(userName, password).ConfigureAwait(false);

            await _connection.StartAsync().ConfigureAwait(false);

            for (int i = 0; i < _addresses.Length; i++)
            {
                var address = _addresses[i];
                var listenerCount = _addressListenerCounts[i];
                var isSession = _isSessions[i];

                await AddQueueClientAsync(listenerCount, address, isSession).ConfigureAwait(false);
            }

            for (int i = 0; i < _topicAddresses.Length; i++)
            {
                var topicAddress = _topicAddresses[i];
                var listenersCount = _topicAddressListenerCounts[i];
                var isTopicSession = _isTopicSessions[i];

                await AddTopicClientAsync(listenersCount, topicAddress, isTopicSession).ConfigureAwait(false);
            }

            Console.WriteLine($"AMQP initialized successfully");
        }

        private async Task AddQueueClientAsync(int listenersCount, string address, bool isSession)
        {
            var queueHost = new QueueHost(listenersCount, address, isSession, this, _connection, _cancellationToken, _logger);

            _queueHostsByAddress.Add(address, queueHost);

            await queueHost.InitializeAsync().ConfigureAwait(false);
        }

        private async Task AddTopicClientAsync(int listenersCount, string address, bool isTopicSession)
        {
            var topicHost = new TopicHost(listenersCount, address, isTopicSession, this, _connection, _cancellationToken, _logger);

            _topicHostsByAddress.Add(address, topicHost);

            await topicHost.InitializeAsync().ConfigureAwait(false);
        }

        public async Task ProcessMessageAsync(string address, string body, string sessionId = null)
        {
            Interlocked.Increment(ref _openTasks);

            try
            {
                if (!string.IsNullOrEmpty(sessionId))
                {
                    await _messageProcessor.ProcessMessagesAsync(address, [body]).ConfigureAwait(false);
                }
                else
                {
                    await _messageProcessor.ProcessMessageAsync(address, body).ConfigureAwait(false);
                }
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

        public async Task SendMessageAsync(string address, AMQPMessage body, double? timeOffsetSeconds = null, string sessionId = null)
        {
            Interlocked.Increment(ref _openTasks);

            try
            {
                if (_topicHostsByAddress.TryGetValue(address, out var topicHost))
                {
                    await topicHost.SendMessageAsync(body, timeOffsetSeconds, sessionId);

                    return;
                }

                if (!_queueHostsByAddress.TryGetValue(address, out var queueHost))
                {
                    throw new Exception($"Address {address} is not available for AMQP sending");
                }

                await queueHost.SendMessageAsync(body, timeOffsetSeconds, sessionId).ConfigureAwait(false);
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

        public async Task SendMessagesAsync<T>(string address, ICollection<T> body, double? timeOffsetSeconds = null, string sessionId = null) where T : AMQPMessage
        {
            Interlocked.Increment(ref _openTasks);

            try
            {
                if (_topicHostsByAddress.TryGetValue(address, out var topicHost))
                {
                    throw new NotImplementedException();
                }

                if (!_queueHostsByAddress.TryGetValue(address, out var queueHost))
                {
                    throw new Exception($"Address {address} is not available for AMQP sending");
                }

                await queueHost.SendMessagesAsync(body, timeOffsetSeconds, sessionId).ConfigureAwait(false);
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

        public Task<bool?> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_connection?.IsStarted);
        }

        public async Task ShutdownReceiversAsync()
        {
            Console.WriteLine("Shutting down AMQP receivers...");

            try
            {
                foreach (var queueHost in _queueHostsByAddress.Values)
                {
                    await queueHost.CloseConsumersAsync(isShuttingDown: true).ConfigureAwait(false);
                }

                foreach (var topicHost in _topicHostsByAddress.Values)
                {
                    await topicHost.CloseConsumersAsync(isShuttingDown: true).ConfigureAwait(false);
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
                await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);

                foreach (var queueHost in _queueHostsByAddress.Values)
                {
                    await queueHost.CloseAsync().ConfigureAwait(false);
                }

                foreach (var topicHost in _topicHostsByAddress.Values)
                {
                    await topicHost.CloseAsync().ConfigureAwait(false);
                }

                if (_connection != null)
                {
                    await _connection.CloseAsync().ConfigureAwait(false);

                    _connection = null;
                }
            }
            catch (Exception)
            {
                // ignore all shutdown faults
            }

            Console.WriteLine("AMQP shut down successfully");
        }
    }
}
