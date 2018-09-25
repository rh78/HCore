using System.Threading.Tasks;

namespace ReinhardHolzner.Core.AMQP.Internal
{
    internal interface IAMQPMessenger<TMessage>
    {
        Task InitializeAddressesAsync(bool useAmqpListener, bool useAmqpSender, string[] addresses, int[] addressListenerCount);

        Task SendMessageAsync(string address, TMessage body);              
    }
}
