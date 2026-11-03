using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;

namespace HCore.Amqp.Sqs
{
    internal interface ISnsApi
    {
        // returns the topic ARN; creating an existing topic returns it unchanged

        Task<string> CreateTopicAsync(string topicName, CancellationToken cancellationToken);

        Task PublishAsync(PublishRequest request, CancellationToken cancellationToken);

        // subscribes the queue with raw message delivery, so it receives the published body unchanged

        Task SubscribeQueueAsync(string topicArn, string queueArn, CancellationToken cancellationToken);
    }
}
