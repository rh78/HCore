using System.Threading.Tasks;

namespace ReinhardHolzner.Core.Amqp.Processor
{
    public interface IAMQPMessenger
    {
        Task InitializeAsync();

        Task SendMessageAsync(string address, AMQPMessage body);              
    }
}
