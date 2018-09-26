using System.Threading.Tasks;

namespace ReinhardHolzner.Core.AMQP.Processor
{
    internal interface IAMQPMessenger
    {
        Task InitializeAsync();

        Task SendMessageAsync(string address, AMQPMessage body);              
    }
}
