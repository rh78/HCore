using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amqp;

namespace ReinhardHolzner.HCore.AMQP.Impl
{
    public abstract class AMQPMessenger : IAMQPMessenger
    {
        private Dictionary<string, SenderLink> _senderLinks = new Dictionary<string, SenderLink>();
        private Dictionary<string, ReceiverLink> _receiverLinks = new Dictionary<string, ReceiverLink>();
        private Dictionary<string, Task> _receiverLinkTasks = new Dictionary<string, Task>();

        internal void AddSenderLink(string address, SenderLink senderLink)
        {
            _senderLinks.Add(address, senderLink);
        }

        internal void AddReceiverLink(string address, ReceiverLink receiverLink)
        {
            _receiverLinks.Add(address, receiverLink);

            Task task = Task.Run(async () =>
            {
                do
                {
                    Message message = await receiverLink.ReceiveAsync(TimeSpan.FromMinutes(60));

                    await ProcessMessageAsync(address, message.Body);
                } while (true);
            });

            _receiverLinkTasks.Add(address, task);
        }

        public async Task SendMessageAsync(string address, object body)
        {
            if (!_senderLinks.ContainsKey(address))
                throw new Exception($"Address {address} is not available for AMQP sending");

            await _senderLinks[address].SendAsync(new Message(body));
        }

        public abstract Task ProcessMessageAsync(string address, object body);        
    }
}
