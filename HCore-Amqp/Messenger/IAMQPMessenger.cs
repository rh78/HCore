using HCore.Amqp.Message;
using System.Threading;
using System.Threading.Tasks;

namespace HCore.Amqp.Messenger
{
    public interface IAMQPMessenger
    {
        Task InitializeAsync();

        Task SendMessageAsync(string address, AMQPMessage body, double? timeOffsetSeconds = null, string sessionId = null);

        Task SendMessageTrySynchronousFirstAsync(string address, AMQPMessage body, double? timeOffsetSeconds = null, string sessionId = null);

        Task<bool> IsAvailableAsync(CancellationToken cancellationToken);
    }
}
