using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;

namespace HCore.Amqp.Sqs
{
    // the SQS calls the messenger makes, so the tests can run it against an in-memory queue

    internal interface ISqsApi
    {
        // returns null when the queue does not exist

        Task<string> GetQueueUrlAsync(string queueName, CancellationToken cancellationToken);

        Task<string> CreateQueueAsync(string queueName, Dictionary<string, string> attributes, CancellationToken cancellationToken);

        Task<Dictionary<string, string>> GetQueueAttributesAsync(string queueUrl, List<string> attributeNames, CancellationToken cancellationToken);

        Task SetQueueAttributesAsync(string queueUrl, Dictionary<string, string> attributes, CancellationToken cancellationToken);

        Task<List<Amazon.SQS.Model.Message>> ReceiveMessagesAsync(ReceiveMessageRequest request, CancellationToken cancellationToken);

        Task SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken);

        // the batch calls return the entries that failed

        Task<List<BatchResultErrorEntry>> SendMessageBatchAsync(SendMessageBatchRequest request, CancellationToken cancellationToken);

        Task<List<BatchResultErrorEntry>> DeleteMessageBatchAsync(DeleteMessageBatchRequest request, CancellationToken cancellationToken);

        Task<List<BatchResultErrorEntry>> ChangeMessageVisibilityBatchAsync(ChangeMessageVisibilityBatchRequest request, CancellationToken cancellationToken);
    }
}
