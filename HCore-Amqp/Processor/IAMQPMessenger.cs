using HCore.Amqp.Message;
using System.Threading.Tasks;

namespace HCore.Amqp.Processor
{
    public interface IAMQPMessenger
    {
        Task InitializeAsync();

        Task SendMessageAsync(string address, AMQPMessage body);              
    }
}
