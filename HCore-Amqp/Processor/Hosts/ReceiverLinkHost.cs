using Amqp;
using HCore.Amqp.Processor.Impl;
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

        public ReceiverLinkHost(ConnectionFactory connectionFactory, string connectionString, string address, AMQP10MessengerImpl messenger, CancellationToken cancellationToken)
            : base(connectionFactory, connectionString, address, cancellationToken)
        {
            _messenger = messenger;
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

                if ((_receiverLink == null || _receiverLink.IsClosed))
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
                            catch (Exception e)
                            {
                                Console.WriteLine($"Exception during processing AMQP message, rejecting: {e}");

                                _receiverLink.Reject(message);
                            }
                        }
                    }
                }
                catch (AmqpException e)
                {
                    if (!CancellationToken.IsCancellationRequested)                    
                        Console.WriteLine($"AMQP exception in receiver link for address {Address}: {e}");

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
