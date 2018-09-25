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
    internal class AMQPMessengerImpl : IAMQPMessenger, IReceiverLinkHostMessageProcessor
    {
        private Dictionary<string, SenderLinkHost> _senderLinks = new Dictionary<string, SenderLinkHost>();
        private Dictionary<string, ReceiverLinkHost> _receiverLinks = new Dictionary<string, ReceiverLinkHost>();
        private HashSet<Task> _messageProcessorTasks = new HashSet<Task>();

        private string _connectionString { get; set; }
        
        private ConnectionFactory _connectionFactory;

        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;

        private IAMQPMessageProcessor _messageProcessor;

        public AMQPMessengerImpl(string connectionString, IApplicationLifetime applicationLifetime, IAMQPMessageProcessor messageProcessor)
        {
            _connectionString = connectionString;

            _connectionFactory = new ConnectionFactory();

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            _messageProcessor = messageProcessor;

            applicationLifetime.ApplicationStopping.Register(OnShutdown);
        }
        
        private void OnShutdown()
        {
            Console.WriteLine("Shutting down AMQP...");

            _cancellationTokenSource.Cancel();

            if (_messageProcessorTasks.Count > 0)
                Task.WaitAll(_messageProcessorTasks.ToArray());

            foreach (ReceiverLinkHost receiverLinkHost in _receiverLinks.Values)
                receiverLinkHost.CloseAsync().Wait();            

            foreach (SenderLinkHost senderLinkHost in _senderLinks.Values)            
                senderLinkHost.CloseAsync().Wait();                            

            Console.WriteLine("AMQP shut down successfully");
        }

        public async Task AddSenderLinkAsync(string address)
        {
            var senderLinkHost = new SenderLinkHost(_connectionFactory, _connectionString, address, _cancellationToken);
            
            _senderLinks.Add(address, senderLinkHost);

            await senderLinkHost.InitializeAsync().ConfigureAwait(false);
        }

        public async Task AddReceiverLinkAsync(string address)
        {
            var receiverLinkHost = new ReceiverLinkHost(_connectionFactory, _connectionString, address, this, _cancellationToken);

            _receiverLinks.Add(address, receiverLinkHost);

            await receiverLinkHost.InitializeAsync().ConfigureAwait(false);

            _messageProcessorTasks.Add(receiverLinkHost.MessageProcessorTask);
        }

        public async Task SendMessageAsync(string address, object body)
        {
            if (!_senderLinks.ContainsKey(address))
                throw new Exception($"Address {address} is not available for AMQP sending");

            await _senderLinks[address].SendMessageAsync(new Message(body)).ConfigureAwait(false);
        }

        public async Task ProcessMessageAsync(string address, object body)
        {
            await _messageProcessor.ProcessMessageAsync(address, body).ConfigureAwait(false);
        }
    }
}
