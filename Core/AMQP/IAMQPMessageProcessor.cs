using System.Threading.Tasks;

namespace ReinhardHolzner.Core.AMQP
{
    public interface IAMQPMessageProcessor<TMessage>
    {
        Task ProcessMessageAsync(string address, TMessage body);
    }
}
