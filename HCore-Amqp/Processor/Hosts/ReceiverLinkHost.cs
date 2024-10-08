﻿using Amqp;
using HCore.Amqp.Exceptions;
using HCore.Amqp.Messenger.Impl;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HCore.Amqp.Processor.Hosts
{
    internal class ReceiverLinkHost : LinkHost
    {
        private ReceiverLink _receiverLink;
        
        private readonly AMQP10MessengerImpl _messenger;

        public Task MessageProcessorTask { get; private set; }

        private ILogger<AMQP10MessengerImpl> _logger;

        public ReceiverLinkHost(ConnectionFactory connectionFactory, string connectionString, string address, AMQP10MessengerImpl messenger, CancellationToken cancellationToken, ILogger<AMQP10MessengerImpl> logger)
            : base(connectionFactory, connectionString, address, cancellationToken)
        {
            _messenger = messenger;

            _logger = logger;
        }
        
        protected override void InitializeLink(Session session)
        {
            _receiverLink = new ReceiverLink(session, $"{Address}-receiver", Address);

            MessageProcessorTask = Task.Run(async () =>
            {
                await RunMessageProcessorAsync().ConfigureAwait(false);
            });            
        }

        private async Task RunMessageProcessorAsync()
        {
            do
            {
                if (CancellationToken.IsCancellationRequested)
                    break;

                if (_receiverLink == null || _receiverLink.IsClosed)
                    await InitializeAsync().ConfigureAwait(false);

                try
                {
                    using (var message = await _receiverLink.ReceiveAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false))
                    {
                        if (message != null)
                        {
                            try
                            {
                                await _messenger.ProcessMessageAsync(Address, (string) message.Body).ConfigureAwait(false);

                                _receiverLink.Accept(message);
                            }
                            catch (RescheduleException)
                            {
                                // no log, this is "wanted"

                                _receiverLink.Release(message);
                            }
                            catch (PostponeException)
                            {
                                // intentionally locking a message. By default, the lock duration is 1 minute
                            }
                            catch (Exception exception)
                            {
                                _logger.LogError($"Exception during processing AMQP message, rejecting: {exception}");

                                _receiverLink.Release(message);
                            }
                        }
                    }
                }
                catch (AmqpException e)
                {
                    if (!CancellationToken.IsCancellationRequested)
                        _logger.LogError($"AMQP exception in receiver link for address {Address}: {e}");

                    await CloseAsync().ConfigureAwait(false);                   
                }                 
            } while (!CancellationToken.IsCancellationRequested);

            // normal end

            await CloseAsync().ConfigureAwait(false);            
        }

        public override async Task CloseAsync()
        {
            await base.CloseAsync().ConfigureAwait(false);

            _receiverLink = null;
            MessageProcessorTask = null;            
        }
    }
}
