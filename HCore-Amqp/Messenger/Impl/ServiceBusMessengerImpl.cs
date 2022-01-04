using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus.Management;
using HCore.Amqp.Processor.Hosts;
using HCore.Amqp.Message;
using HCore.Amqp.Processor;
using Newtonsoft.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HCore.Amqp.Messenger.Impl
{
    internal class ServiceBusMessengerImpl : IAMQPMessenger
    {
        private readonly Dictionary<string, QueueClientHost> _queueClientHosts = new Dictionary<string, QueueClientHost>();

        private readonly string _connectionString;       

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _cancellationToken;

        private readonly IAMQPMessageProcessor _messageProcessor;

        private readonly string[] _addresses;
        private readonly int[] _addressListenerCounts;
        private readonly bool[] _isSessions;

        private readonly ILogger<ServiceBusMessengerImpl> _logger;

        public ServiceBusMessengerImpl(string connectionString, string[] addresses, int[] addressListenerCount, bool[] isSessions, IHostApplicationLifetime applicationLifetime, IAMQPMessageProcessor messageProcessor, ILogger<ServiceBusMessengerImpl> logger)
        {
            _connectionString = connectionString;

            _addresses = addresses;
            _addressListenerCounts = addressListenerCount;
            _isSessions = isSessions;

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
                string address = _addresses[i];
                int listenerCount = _addressListenerCounts[i];
                bool isSession = _isSessions[i];

                if (!await managementClient.QueueExistsAsync(address).ConfigureAwait(false))
                {
                    Console.WriteLine($"Creating AMQP queue {address}...");

                    await managementClient.CreateQueueAsync(new QueueDescription(address)
                    {
                        LockDuration = TimeSpan.FromMinutes(1),
                        MaxDeliveryCount = Int32.MaxValue,
                        EnablePartitioning = true,
                        MaxSizeInMB = 2048,
                        RequiresSession = isSession
                    }).ConfigureAwait(false);

                    Console.WriteLine($"Created AMQP queue {address}");
                }

                await AddQueueClientAsync(listenerCount, address, isSession).ConfigureAwait(false);
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

        private void OnShutdown()
        {
            Console.WriteLine("Shutting down AMQP...");

            try { 
                _cancellationTokenSource.Cancel();

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                foreach (QueueClientHost queueClientHost in _queueClientHosts.Values)
                    queueClientHost.CloseAsync().Wait();
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
            if (!_queueClientHosts.ContainsKey(address))
                throw new Exception($"Address {address} is not available for AMQP sending");

            await _queueClientHosts[address].SendMessageAsync(body, timeOffsetSeconds, sessionId).ConfigureAwait(false);
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
    }
}
