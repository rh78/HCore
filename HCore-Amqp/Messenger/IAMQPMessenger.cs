using HCore.Amqp.Message;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HCore.Amqp.Messenger
{
    public interface IAMQPMessenger
    {
        Task InitializeAsync();

        Task SendMessageAsync(string address, AMQPMessage body, double? timeOffsetSeconds = null, string sessionId = null);

        Task SendMessagesAsync<T>(string address, ICollection<T> body, double? timeOffsetSeconds = null, string sessionId = null) where T : AMQPMessage;

        Task SendMessageTrySynchronousFirstAsync(string address, AMQPMessage body, double? timeOffsetSeconds = null, string sessionId = null);

        Task<bool?> IsAvailableAsync(CancellationToken cancellationToken = default);

        Task ShutdownReceiversAsync();

        Task WaitForTaskCompletionAsync();

        Task ShutdownAsync();
    }
}
