using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amqp;
using HCore.Amqp.Processor.Hosts;
using HCore.Amqp.Message;
using HCore.Amqp.Processor;
using Newtonsoft.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HCore.Amqp.Messenger.Impl
{
    internal class AMQP10MessengerImpl : IAMQPMessenger
    {
        private readonly Dictionary<string, SenderLinkHost> _senderLinks = new Dictionary<string, SenderLinkHost>();
        private readonly List<ReceiverLinkHost> _receiverLinks = new List<ReceiverLinkHost>();
        private readonly HashSet<Task> _messageProcessorTasks = new HashSet<Task>();

        private readonly string _connectionString;
        
        private readonly ConnectionFactory _connectionFactory;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _cancellationToken;

        private readonly IAMQPMessageProcessor _messageProcessor;

        private readonly string[] _addresses;
        private readonly int[] _addressListenerCounts;

        private readonly ILogger<AMQP10MessengerImpl> _logger;

        public AMQP10MessengerImpl(string connectionString, string[] addresses, int[] addressListenerCount, IHostApplicationLifetime applicationLifetime, IAMQPMessageProcessor messageProcessor, ILogger<AMQP10MessengerImpl> logger)
        {
            _connectionString = connectionString;

            _addresses = addresses;
            _addressListenerCounts = addressListenerCount;

            _connectionFactory = new ConnectionFactory();

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            _messageProcessor = messageProcessor;

            _logger = logger;

            applicationLifetime.ApplicationStopping.Register(OnShutdown);
        }

        public async Task InitializeAsync()
        {
            Console.WriteLine("Initializing AMQP receiver...");

            for (int i = 0; i < _addresses.Length; i++)
            {
                string address = _addresses[i];

                for (int y = 0; y < _addressListenerCounts[i]; y++)                    
                    await AddReceiverLinkAsync(address).ConfigureAwait(false);                    
            }

            Console.WriteLine($"AMQP receiver initialized successfully");            

            Console.WriteLine("Initializing AMQP sender...");

            foreach (string address in _addresses)
                await AddSenderLinkAsync(address).ConfigureAwait(false);

            Console.WriteLine("AMQP sender initialized successfully");            
        }

        private void OnShutdown()
        {
            Console.WriteLine("Shutting down AMQP...");

            try
            {
                _cancellationTokenSource.Cancel();

                if (_messageProcessorTasks.Count > 0)
                    Task.WaitAll(_messageProcessorTasks.ToArray());

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                foreach (ReceiverLinkHost receiverLinkHost in _receiverLinks)
                    receiverLinkHost.CloseAsync().Wait();

                foreach (SenderLinkHost senderLinkHost in _senderLinks.Values)
                    senderLinkHost.CloseAsync().Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
            }
            catch (Exception)
            {
                // ignore all shutdown faults
            }

            Console.WriteLine("AMQP shut down successfully");
        }

        private async Task AddSenderLinkAsync(string address)
        {
            var senderLinkHost = new SenderLinkHost(_connectionFactory, _connectionString, address, _cancellationToken, _logger);
            
            _senderLinks.Add(address, senderLinkHost);

            await senderLinkHost.InitializeAsync().ConfigureAwait(false);
        }

        private async Task AddReceiverLinkAsync(string address)
        {
            var receiverLinkHost = new ReceiverLinkHost(_connectionFactory, _connectionString, address, this, _cancellationToken, _logger);

            _receiverLinks.Add(receiverLinkHost);

            await receiverLinkHost.InitializeAsync().ConfigureAwait(false);

            _messageProcessorTasks.Add(receiverLinkHost.MessageProcessorTask);
        }

        public async Task SendMessageAsync(string address, AMQPMessage body, double? timeOffsetSeconds = null)
        {
            if (!_senderLinks.ContainsKey(address))
                throw new Exception($"Address {address} is not available for AMQP sending");

            await _senderLinks[address].SendMessageAsync(body, timeOffsetSeconds).ConfigureAwait(false);
        }

        public async Task SendMessageTrySynchronousFirstAsync(string address, AMQPMessage body, double? timeOffsetSeconds = null)
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

                    await SendMessageAsync(address, body, timeOffsetSeconds).ConfigureAwait(false);
                }
            }
        }

        public async Task ProcessMessageAsync(string address, string messageBodyJson)
        {
            await _messageProcessor.ProcessMessageAsync(address, messageBodyJson).ConfigureAwait(false);
        }        
    }
}
