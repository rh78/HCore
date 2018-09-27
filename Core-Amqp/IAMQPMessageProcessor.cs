using System.Threading.Tasks;

namespace ReinhardHolzner.Core.Amqp
{
    public interface IAMQPMessageProcessor
    {
        Task<bool> ProcessMessageAsync(string address, string messageBodyJson);
    }
}
