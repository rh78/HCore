using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.ServiceBus.Management;
using ReinhardHolzner.Core.AMQP.Processor.Hosts;

namespace ReinhardHolzner.Core.AMQP.Processor.Impl
{
    internal class ServiceBusMessengerImpl : IAMQPMessenger
    {
        private Dictionary<string, QueueClientHost> _queueClientHosts = new Dictionary<string, QueueClientHost>();

        private string _connectionString;       

        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;

        private IAMQPMessageProcessor _messageProcessor;

        private string[] _addresses;
        private int[] _addressListenerCounts;

        public ServiceBusMessengerImpl(string connectionString, string[] addresses, int[] addressListenerCount, IApplicationLifetime applicationLifetime, IAMQPMessageProcessor messageProcessor)
        {
            _connectionString = connectionString;

            _addresses = addresses;
            _addressListenerCounts = addressListenerCount;

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            _messageProcessor = messageProcessor;

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

                if (!await managementClient.QueueExistsAsync(address).ConfigureAwait(false))
                {
                    Console.WriteLine($"Creating AMQP queue {address}...");

                    await managementClient.CreateQueueAsync(new QueueDescription(address)
                    {
                        LockDuration = TimeSpan.FromMinutes(1),
                        MaxDeliveryCount = Int32.MaxValue,
                        EnablePartitioning = true,
                        MaxSizeInMB = 2048                        
                    }).ConfigureAwait(false);

                    Console.WriteLine($"Created AMQP queue {address}");
                }

                await AddQueueClientAsync(listenerCount, address).ConfigureAwait(false);
            }

            await managementClient.CloseAsync().ConfigureAwait(false);

            Console.WriteLine($"AMQP initialized successfully");
        }

        private async Task AddQueueClientAsync(int amqpListenerCount, string address)
        {
            var queueClientHost = new QueueClientHost(_connectionString, amqpListenerCount, address, this, _cancellationToken);

            _queueClientHosts.Add(address, queueClientHost);

            await queueClientHost.InitializeAsync().ConfigureAwait(false);            
        }

        private void OnShutdown()
        {
            Console.WriteLine("Shutting down AMQP...");

            try { 
                _cancellationTokenSource.Cancel();

                foreach (QueueClientHost queueClientHost in _queueClientHosts.Values)
                    queueClientHost.CloseAsync().Wait();
            }
            catch (Exception)
            {
                // ignore all shutdown faults
            }

            Console.WriteLine("AMQP shut down successfully");
        }

        public async Task SendMessageAsync(string address, AMQPMessage body)
        {
            if (!_queueClientHosts.ContainsKey(address))
                throw new Exception($"Address {address} is not available for AMQP sending");

            await _queueClientHosts[address].SendMessageAsync(body).ConfigureAwait(false);
        }

        public async Task ProcessMessageAsync(string address, string messageBodyJson)
        {
            await _messageProcessor.ProcessMessageAsync(address, messageBodyJson).ConfigureAwait(false);
        }
    }
}
