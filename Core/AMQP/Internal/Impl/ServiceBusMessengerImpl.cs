using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.ServiceBus.Management;
using ReinhardHolzner.Core.AMQP.Internal.Hosts;

namespace ReinhardHolzner.Core.AMQP.Internal.Impl
{
    internal class ServiceBusMessengerImpl<TMessage> : IAMQPMessenger<TMessage>
    {
        private Dictionary<string, QueueClientHost<TMessage>> _queueClientHosts = new Dictionary<string, QueueClientHost<TMessage>>();

        private string _connectionString;       

        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;

        private IAMQPMessageProcessor<TMessage> _messageProcessor;

        public ServiceBusMessengerImpl(string connectionString, IApplicationLifetime applicationLifetime, IAMQPMessageProcessor<TMessage> messageProcessor)
        {
            _connectionString = connectionString;            

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            _messageProcessor = messageProcessor;

            applicationLifetime.ApplicationStopping.Register(OnShutdown);
        }

        public async Task InitializeAddressesAsync(bool useAmqpListener, bool useAmqpSender, string[] addresses, int[] addressListenerCount)
        {
            Console.WriteLine("Initializing AMQP...");

            var managementClient = new ManagementClient(_connectionString);

            for (int i = 0; i < addresses.Length; i++)
            {
                string address = addresses[i];
                int listenerCount = addressListenerCount[i];

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
            var queueClientHost = new QueueClientHost<TMessage>(_connectionString, amqpListenerCount, address, this, _cancellationToken);

            _queueClientHosts.Add(address, queueClientHost);

            await queueClientHost.InitializeAsync().ConfigureAwait(false);            
        }

        private void OnShutdown()
        {
            Console.WriteLine("Shutting down AMQP...");

            _cancellationTokenSource.Cancel();

            foreach (QueueClientHost<TMessage> queueClientHost in _queueClientHosts.Values)
                queueClientHost.CloseAsync().Wait();
            
            Console.WriteLine("AMQP shut down successfully");
        }

        public async Task SendMessageAsync(string address, TMessage body)
        {
            if (!_queueClientHosts.ContainsKey(address))
                throw new Exception($"Address {address} is not available for AMQP sending");

            await _queueClientHosts[address].SendMessageAsync(body).ConfigureAwait(false);
        }

        public async Task ProcessMessageAsync(string address, TMessage body)
        {
            await _messageProcessor.ProcessMessageAsync(address, body).ConfigureAwait(false);
        }
    }
}
