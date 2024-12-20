﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amqp;
using HCore.Amqp.Processor.Hosts;
using HCore.Amqp.Message;
using HCore.Amqp.Processor;
using Newtonsoft.Json;
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

        private int _openTasks = 0;

        public AMQP10MessengerImpl(string connectionString, string[] addresses, int[] addressListenerCount, IAMQPMessageProcessor messageProcessor, ILogger<AMQP10MessengerImpl> logger)
        {
            _connectionString = connectionString;

            _addresses = addresses;
            _addressListenerCounts = addressListenerCount;

            _connectionFactory = new ConnectionFactory();

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            _messageProcessor = messageProcessor;

            _logger = logger;
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

        public async Task ShutdownReceiversAsync()
        {
            Console.WriteLine("Shutting down AMQP receivers...");

            try
            {
                foreach (ReceiverLinkHost receiverLinkHost in _receiverLinks)
                {
                    await receiverLinkHost.CloseAsync().ConfigureAwait(false);
                }

                _receiverLinks.Clear();
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

                if (_messageProcessorTasks.Count > 0)
                {
                    await Task.WhenAll(_messageProcessorTasks.ToArray()).ConfigureAwait(false);
                }

                foreach (ReceiverLinkHost receiverLinkHost in _receiverLinks)
                {
                    await receiverLinkHost.CloseAsync().ConfigureAwait(false);
                }

                foreach (SenderLinkHost senderLinkHost in _senderLinks.Values)
                {
                    await senderLinkHost.CloseAsync().ConfigureAwait(false);
                }
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

        public async Task SendMessageAsync(string address, AMQPMessage body, double? timeOffsetSeconds = null, string sessionId = null)
        {
            Interlocked.Increment(ref _openTasks);

            try
            {
                if (!_senderLinks.ContainsKey(address))
                    throw new Exception($"Address {address} is not available for AMQP sending");

                if (!string.IsNullOrEmpty(sessionId))
                    throw new Exception("Session ID is not supported by the AMQP 1.0 implementation");

                await _senderLinks[address].SendMessageAsync(body, timeOffsetSeconds).ConfigureAwait(false);
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

        public Task SendMessagesAsync<T>(string address, ICollection<T> body, double? timeOffsetSeconds = null, string sessionId = null) where T : AMQPMessage
        {
            throw new NotImplementedException();
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
            catch (Exception)
            {
                throw;
            }
            finally
            {
                Interlocked.Decrement(ref _openTasks);
            }
        }

        public Task<bool?> IsAvailableAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<bool?>(true);
        }
    }
}
