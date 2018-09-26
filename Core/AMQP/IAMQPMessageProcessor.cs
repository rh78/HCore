using System.Threading.Tasks;

namespace ReinhardHolzner.Core.AMQP
{
    public interface IAMQPMessageProcessor
    {
        Task<bool> ProcessMessageAsync(string address, string messageBodyJson);
    }
}
