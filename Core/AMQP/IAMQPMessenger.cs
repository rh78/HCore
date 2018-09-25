using Amqp;
using System.Threading.Tasks;

namespace ReinhardHolzner.Core.AMQP
{
    public interface IAMQPMessenger
    {
        Task SendMessageAsync(string address, object body);
        Task ProcessMessageAsync(string address, object body);

        void AddSenderLink(string address, SenderLink senderLink);
        void AddReceiverLink(string address, ReceiverLink receiverLink);        
    }
}
