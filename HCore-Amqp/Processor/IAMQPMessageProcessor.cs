using System.Threading.Tasks;

namespace HCore.Amqp.Processor
{
    public interface IAMQPMessageProcessor
    {
        Task<bool> ProcessMessageAsync(string address, string messageBodyJson);
    }
}
