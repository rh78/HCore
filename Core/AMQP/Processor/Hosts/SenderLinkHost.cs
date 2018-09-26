using Amqp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ReinhardHolzner.Core.AMQP.Processor.Hosts
{
    internal class SenderLinkHost<TMessage> : LinkHost
    {
        private SenderLink _senderLink;

        public SenderLinkHost(ConnectionFactory connectionFactory, string connectionString, string address, CancellationToken cancellationToken)
            : base(connectionFactory, connectionString, address, cancellationToken)
        {
            
        }

        protected override void InitializeLink(Session session)
        {
            _senderLink = new SenderLink(session, $"{Address}-sender", Address);
        }

        public async Task SendMessageAsync(TMessage messageBody)
        {
            try
            {
                if (CancellationToken.IsCancellationRequested)
                    throw new Exception("AMQP cancellation is requested, can not send message");

                if (_senderLink == null || _senderLink.IsClosed)
                    await InitializeAsync().ConfigureAwait(false);

                Message message = new Message(messageBody);

                message.Header.Durable = true;

                await _senderLink.SendAsync(message, TimeSpan.FromSeconds(10)).ConfigureAwait(false);                
            } catch (AmqpException e)
            {
                if (!CancellationToken.IsCancellationRequested)
                    Console.WriteLine($"AMQP exception in sender link for address {Address}: {e}");

                await CloseAsync().ConfigureAwait(false);

                if (!CancellationToken.IsCancellationRequested)
                    await SendMessageAsync(messageBody).ConfigureAwait(false);                
            }
        }

        public override async Task CloseAsync()
        {
            await base.CloseAsync().ConfigureAwait(false);

            _senderLink = null;
        }
    }
}
