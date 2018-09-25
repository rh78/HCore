using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amqp;
using Microsoft.AspNetCore.Hosting;
using ReinhardHolzner.Core.AMQP.Internal.Hosts;

namespace ReinhardHolzner.Core.AMQP.Internal.Impl
{
    internal class AMQP10MessengerImpl<TMessage> : IAMQPMessenger<TMessage>
    {
        private Dictionary<string, SenderLinkHost<TMessage>> _senderLinks = new Dictionary<string, SenderLinkHost<TMessage>>();
        private List<ReceiverLinkHost<TMessage>> _receiverLinks = new List<ReceiverLinkHost<TMessage>>();
        private HashSet<Task> _messageProcessorTasks = new HashSet<Task>();

        private string _connectionString;
        
        private ConnectionFactory _connectionFactory;

        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;

        private IAMQPMessageProcessor<TMessage> _messageProcessor;

        public AMQP10MessengerImpl(string connectionString, IApplicationLifetime applicationLifetime, IAMQPMessageProcessor<TMessage> messageProcessor)
        {
            _connectionString = connectionString;            

            _connectionFactory = new ConnectionFactory();

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            _messageProcessor = messageProcessor;

            applicationLifetime.ApplicationStopping.Register(OnShutdown);
        }

        public async Task InitializeAddressesAsync(bool useAmqpListener, bool useAmqpSender, string[] addresses, int[] addressListenerCounts)
        {
            if (useAmqpListener)
            {
                Console.WriteLine("Initializing AMQP receiver...");

                for (int i = 0; i < addresses.Length; i++)
                {
                    string address = addresses[i];

                    for (int y = 0; y < addressListenerCounts[i]; y++)                    
                        await AddReceiverLinkAsync(address).ConfigureAwait(false);                    
                }

                Console.WriteLine($"AMQP receiver initialized successfully");
            }

            if (useAmqpSender)
            {
                Console.WriteLine("Initializing AMQP sender...");

                foreach (string address in addresses)
                    await AddSenderLinkAsync(address).ConfigureAwait(false);

                Console.WriteLine("AMQP sender initialized successfully");
            }
        }

        private void OnShutdown()
        {
            Console.WriteLine("Shutting down AMQP...");

            _cancellationTokenSource.Cancel();

            if (_messageProcessorTasks.Count > 0)
                Task.WaitAll(_messageProcessorTasks.ToArray());

            foreach (ReceiverLinkHost<TMessage> receiverLinkHost in _receiverLinks)
                receiverLinkHost.CloseAsync().Wait();            

            foreach (SenderLinkHost<TMessage> senderLinkHost in _senderLinks.Values)            
                senderLinkHost.CloseAsync().Wait();                            

            Console.WriteLine("AMQP shut down successfully");
        }

        private async Task AddSenderLinkAsync(string address)
        {
            var senderLinkHost = new SenderLinkHost<TMessage>(_connectionFactory, _connectionString, address, _cancellationToken);
            
            _senderLinks.Add(address, senderLinkHost);

            await senderLinkHost.InitializeAsync().ConfigureAwait(false);
        }

        private async Task AddReceiverLinkAsync(string address)
        {
            var receiverLinkHost = new ReceiverLinkHost<TMessage>(_connectionFactory, _connectionString, address, this, _cancellationToken);

            _receiverLinks.Add(receiverLinkHost);

            await receiverLinkHost.InitializeAsync().ConfigureAwait(false);

            _messageProcessorTasks.Add(receiverLinkHost.MessageProcessorTask);
        }

        public async Task SendMessageAsync(string address, TMessage body)
        {
            if (!_senderLinks.ContainsKey(address))
                throw new Exception($"Address {address} is not available for AMQP sending");

            await _senderLinks[address].SendMessageAsync(body).ConfigureAwait(false);
        }

        public async Task ProcessMessageAsync(string address, TMessage body)
        {
            await _messageProcessor.ProcessMessageAsync(address, body).ConfigureAwait(false);
        }        
    }
}
