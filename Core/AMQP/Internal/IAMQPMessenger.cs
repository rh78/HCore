using System.Threading.Tasks;

namespace ReinhardHolzner.Core.AMQP.Internal
{
    internal interface IAMQPMessenger
    {
        Task SendMessageAsync(string address, object body);        

        Task AddSenderLinkAsync(string address);
        Task AddReceiverLinkAsync (string address);        
    }
}
