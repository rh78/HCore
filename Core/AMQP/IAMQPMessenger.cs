using System.Threading.Tasks;

namespace ReinhardHolzner.HCore.AMQP
{
    public interface IAMQPMessenger
    {
        Task SendMessageAsync(string address, object body);
        Task ProcessMessageAsync(string address, object body);        
    }
}
