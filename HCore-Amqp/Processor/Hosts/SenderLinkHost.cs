using Amqp;
using HCore.Amqp.Message;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HCore.Amqp.Processor.Hosts
{
    internal class SenderLinkHost : LinkHost
    {
        private SenderLink _senderLink;

        private readonly static SemaphoreSlim semaphore = new SemaphoreSlim(0, 1);

        public SenderLinkHost(ConnectionFactory connectionFactory, string connectionString, string address, CancellationToken cancellationToken)
            : base(connectionFactory, connectionString, address, cancellationToken)
        {
            
        }

        protected override void InitializeLink(Session session)
        {
            _senderLink = new SenderLink(session, $"{Address}-sender", Address);
        }

        public async Task SendMessageAsync(AMQPMessage messageBody)
        {
            SenderLink senderLink;
            bool error;

            do
            {
                senderLink = _senderLink;
                error = false;

                if (CancellationToken.IsCancellationRequested)
                    throw new Exception("AMQP cancellation is requested, can not send message");

                try
                {
                    if (senderLink == null || senderLink.IsClosed)
                        await InitializeAsync().ConfigureAwait(false);

                    global::Amqp.Message message = new global::Amqp.Message(JsonConvert.SerializeObject(messageBody))
                    {
                        Header = new global::Amqp.Framing.Header()
                        {
                            Durable = true
                        }
                    };

                    await senderLink.SendAsync(message, TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                }
                catch (AmqpException e)
                {
                    error = true;

                    await semaphore.WaitAsync().ConfigureAwait(false);

                    if (senderLink == _senderLink)
                    {
                        // nobody else handled this before

                        if (!CancellationToken.IsCancellationRequested)
                            Console.WriteLine($"AMQP exception in sender link for address {Address}: {e}");

                        await CloseAsync().ConfigureAwait(false);
                    }

                    semaphore.Release();
                }
            } while (error);
        }

        public override async Task CloseAsync()
        {
            await base.CloseAsync().ConfigureAwait(false);

            _senderLink = null;
        }
    }
}
