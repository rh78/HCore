using System.Threading.Tasks;

namespace ReinhardHolzner.Core.AMQP
{
    public interface IAMQPMessageProcessor
    {
        Task ProcessMessageAsync(string address, object body);
    }
}
