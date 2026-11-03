using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HCore.Amqp.Message;
using HCore.Amqp.Processor;
using HCore.Amqp.Processor.Hosts;
using HCore.Amqp.Sqs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HCore.Amqp.Messenger.Impl
{
    // Session queues are SQS FIFO queues (<address>.fifo, message group = session ID), the other queues are standard SQS
    // queues, topics are SNS topics. Without CreateResources in the connection string, all of them must exist.

    internal class SqsMessengerImpl : IAMQPMessenger
    {
        private readonly Dictionary<string, SqsQueueHost> _queueHostsByAddress = [];
        private readonly Dictionary<string, SnsTopicHost> _topicHostsByAddress = [];

        private readonly SqsConnectionSettings _connectionSettings;

        private readonly CancellationTokenSource _cancellationTokenSource;

        private readonly IAMQPMessageProcessor _messageProcessor;

        private readonly string[] _addresses;
        private readonly string[] _topicAddresses;

        private readonly int[] _addressListenerCounts;
        private readonly int[] _topicAddressListenerCounts;

        private readonly bool[] _isSessions;

        private readonly int?[] _maxReceiveCounts;

        private readonly string _machineName;

        private readonly AwsMessagingApi _awsMessagingApi;

        private int _openTasks = 0;

        internal ISqsApi SqsApi { get; }
        internal ISnsApi SnsApi { get; }

        internal SqsTimings Timings { get; }

        internal CancellationToken CancellationToken { get; }

        internal ILogger<SqsMessengerImpl> Logger { get; }

        internal bool CreateResources => _connectionSettings.CreateResources;

        internal string Region => _connectionSettings.Region;

        public SqsMessengerImpl(
            string connectionString,
            string queueNamePrefix,
            string[] addresses,
            string[] topicAddresses,
            int[] addressListenerCount,
            int[] topicAddressListenerCount,
            bool[] isSessions,
            bool[] isTopicSessions,
            int?[] maxReceiveCounts,
            IAMQPMessageProcessor messageProcessor,
            ILogger<SqsMessengerImpl> logger)
            : this(SqsConnectionSettings.Parse(connectionString, queueNamePrefix), sqsApi: null, snsApi: null, new SqsTimings(), Environment.MachineName,
                  addresses, topicAddresses, addressListenerCount, topicAddressListenerCount, isSessions, isTopicSessions, maxReceiveCounts, messageProcessor, logger)
        {
        }

        // the tests pass their own SQS and SNS implementations, timings and machine name

        internal SqsMessengerImpl(
            SqsConnectionSettings connectionSettings,
            ISqsApi sqsApi,
            ISnsApi snsApi,
            SqsTimings timings,
            string machineName,
            string[] addresses,
            string[] topicAddresses,
            int[] addressListenerCount,
            int[] topicAddressListenerCount,
            bool[] isSessions,
            bool[] isTopicSessions,
            int?[] maxReceiveCounts,
            IAMQPMessageProcessor messageProcessor,
            ILogger<SqsMessengerImpl> logger)
        {
            if (isTopicSessions.Any(isTopicSession => isTopicSession))
            {
                throw new Exception("AMQP SQS implementation does not support topic sessions");
            }

            _connectionSettings = connectionSettings;

            if (sqsApi == null || snsApi == null)
            {
                _awsMessagingApi = new AwsMessagingApi(connectionSettings);

                sqsApi = _awsMessagingApi;
                snsApi = _awsMessagingApi;
            }

            SqsApi = sqsApi;
            SnsApi = snsApi;

            Timings = timings;

            _machineName = machineName;

            _addresses = addresses;
            _topicAddresses = topicAddresses;
            _addressListenerCounts = addressListenerCount;
            _topicAddressListenerCounts = topicAddressListenerCount;
            _isSessions = isSessions;
            _maxReceiveCounts = maxReceiveCounts;

            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken = _cancellationTokenSource.Token;

            _messageProcessor = messageProcessor;

            Logger = logger;
        }

        public async Task InitializeAsync()
        {
            Console.WriteLine("Initializing AMQP...");

            var queueNamePrefix = _connectionSettings.QueueNamePrefix;

            for (int i = 0; i < _addresses.Length; i++)
            {
                var address = _addresses[i];
                var isSession = _isSessions[i];

                var queueHost = new SqsQueueHost(address, SqsNames.GetQueueName(queueNamePrefix, address, isFifo: isSession), isFifo: isSession, _addressListenerCounts[i], _maxReceiveCounts[i], topicArn: null, this);

                await queueHost.InitializeAsync().ConfigureAwait(false);

                _queueHostsByAddress.Add(address, queueHost);
            }

            string accountId = null;

            if (_topicAddresses.Length > 0 && !CreateResources)
            {
                var queueHost = _queueHostsByAddress.Values.FirstOrDefault()
                    ?? throw new Exception("AMQP SQS implementation needs a queue address to find the account of the SNS topics");

                accountId = SqsNames.GetAccountId(queueHost.QueueUrl);
            }

            for (int i = 0; i < _topicAddresses.Length; i++)
            {
                var topicAddress = _topicAddresses[i];

                var topicHost = new SnsTopicHost(topicAddress, SqsNames.GetTopicName(queueNamePrefix, topicAddress), SqsNames.GetTopicQueueName(queueNamePrefix, topicAddress, _machineName), _topicAddressListenerCounts[i], this);

                await topicHost.InitializeAsync(accountId).ConfigureAwait(false);

                _topicHostsByAddress.Add(topicAddress, topicHost);
            }

            // listening starts once every address can be sent to, since processing a message may send to another address

            foreach (var queueHost in _queueHostsByAddress.Values)
            {
                queueHost.StartListeners();
            }

            foreach (var topicHost in _topicHostsByAddress.Values)
            {
                topicHost.StartListeners();
            }

            Console.WriteLine($"AMQP initialized successfully");
        }

        internal async Task SendWithRetryAsync(string address, Func<CancellationToken, Task> sendAsync)
        {
            while (true)
            {
                if (CancellationToken.IsCancellationRequested)
                {
                    throw new Exception("AMQP cancellation is requested, can not send message");
                }

                try
                {
                    await sendAsync(CancellationToken).ConfigureAwait(false);

                    return;
                }
                catch (Exception e)
                {
                    if (!CancellationToken.IsCancellationRequested)
                    {
                        Logger.LogError($"AMQP exception in sender for address {address}: {e}");
                    }
                }

                try
                {
                    await Task.Delay(Timings.RetryDelay, CancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // checked at the start of the loop
                }
            }
        }

        public async Task SendMessageAsync(string address, AMQPMessage body, double? timeOffsetSeconds = null, string sessionId = null)
        {
            Interlocked.Increment(ref _openTasks);

            try
            {
                if (_topicHostsByAddress.TryGetValue(address, out var topicHost))
                {
                    await topicHost.SendMessageAsync(body, timeOffsetSeconds).ConfigureAwait(false);

                    return;
                }

                if (!_queueHostsByAddress.TryGetValue(address, out var queueHost))
                {
                    throw new Exception($"Address {address} is not available for AMQP sending");
                }

                await queueHost.SendMessageAsync(body, timeOffsetSeconds, sessionId).ConfigureAwait(false);
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
                if (_topicHostsByAddress.ContainsKey(address))
                {
                    throw new NotImplementedException();
                }

                if (!_queueHostsByAddress.TryGetValue(address, out var queueHost))
                {
                    throw new Exception($"Address {address} is not available for AMQP sending");
                }

                await queueHost.SendMessagesAsync(body, timeOffsetSeconds, sessionId).ConfigureAwait(false);
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
                if (!CancellationToken.IsCancellationRequested)
                {
                    Logger.LogError($"AMQP exception in sender link for address {address}: {e}");

                    await SendMessageAsync(address, body, timeOffsetSeconds, sessionId).ConfigureAwait(false);
                }
            }
            finally
            {
                Interlocked.Decrement(ref _openTasks);
            }
        }

        internal async Task ProcessMessageAsync(string address, string messageBodyJson)
        {
            Interlocked.Increment(ref _openTasks);

            try
            {
                await _messageProcessor.ProcessMessageAsync(address, messageBodyJson).ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref _openTasks);
            }
        }

        internal async Task ProcessMessagesAsync(string address, List<string> messageBodyJsons)
        {
            Interlocked.Increment(ref _openTasks);

            try
            {
                await _messageProcessor.ProcessMessagesAsync(address, messageBodyJsons).ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref _openTasks);
            }
        }

        public async Task<bool?> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_addresses.Length == 0)
                {
                    return false;
                }

                var queueName = SqsNames.GetQueueName(_connectionSettings.QueueNamePrefix, _addresses[0], _isSessions[0]);

                return await SqsApi.GetQueueUrlAsync(queueName, cancellationToken).ConfigureAwait(false) != null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task ShutdownReceiversAsync()
        {
            Console.WriteLine("Shutting down AMQP receivers...");

            try
            {
                foreach (var queueHost in _queueHostsByAddress.Values)
                {
                    await queueHost.CancelReceiversAsync().ConfigureAwait(false);
                }

                foreach (var topicHost in _topicHostsByAddress.Values)
                {
                    await topicHost.CancelReceiversAsync().ConfigureAwait(false);
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

                _awsMessagingApi?.Dispose();
            }
            catch (Exception)
            {
                // ignore all shutdown faults
            }

            Console.WriteLine("AMQP shut down successfully");
        }
    }
}
