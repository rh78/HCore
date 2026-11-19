using HCore.Amqp.Message;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HCore.Amqp.Messenger
{
    public interface IAMQPMessenger
    {
        Task InitializeAsync();

        // The message group is used by SQS only, where every queue except the topic queues is a FIFO queue: the messages of a
        // group are processed one after another, and a message without a group gets a group of its own. A session queue groups
        // by the session ID instead. Service Bus and ActiveMQ ignore the group.

        Task SendMessageAsync(string address, AMQPMessage body, double? timeOffsetSeconds = null, string sessionId = null, string messageGroupId = null);

        Task SendMessagesAsync<T>(string address, ICollection<T> body, double? timeOffsetSeconds = null, string sessionId = null, Func<T, string> getMessageGroupId = null) where T : AMQPMessage;

        Task SendMessageTrySynchronousFirstAsync(string address, AMQPMessage body, double? timeOffsetSeconds = null, string sessionId = null, string messageGroupId = null);

        Task<bool?> IsAvailableAsync(CancellationToken cancellationToken = default);

        Task ShutdownReceiversAsync();

        Task WaitForTaskCompletionAsync();

        Task ShutdownAsync();
    }
}
