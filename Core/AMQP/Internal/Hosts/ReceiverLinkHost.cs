using Amqp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ReinhardHolzner.Core.AMQP.Internal.Hosts
{
    internal class ReceiverLinkHost : LinkHost
    {
        private ReceiverLink _receiverLink;

        private IReceiverLinkHostMessageProcessor _messageProcessor;

        public Task MessageProcessorTask { get; private set; }

        public ReceiverLinkHost(ConnectionFactory connectionFactory, string connectionString, string address, IReceiverLinkHostMessageProcessor messageProcessor, CancellationToken cancellationToken)
            : base(connectionFactory, connectionString, address, cancellationToken)
        {
            _messageProcessor = messageProcessor;
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
                    Message message = await _receiverLink.ReceiveAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

                    if (message != null && message.Body != null)
                        await _messageProcessor.ProcessMessageAsync(Address, message.Body).ConfigureAwait(false);
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
