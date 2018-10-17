using System.Threading.Tasks;

namespace HCore.Amqp
{
    public interface IAMQPMessageProcessor
    {
        Task<bool> ProcessMessageAsync(string address, string messageBodyJson);
    }
}
