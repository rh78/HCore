using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using SqsBatchResultErrorEntry = Amazon.SQS.Model.BatchResultErrorEntry;

namespace HCore.Amqp.Sqs
{
    internal class AwsMessagingApi : ISqsApi, ISnsApi, IDisposable
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly IAmazonSimpleNotificationService _snsClient;

        internal AwsMessagingApi(SqsConnectionSettings connectionSettings)
        {
            var sqsConfig = new AmazonSQSConfig();
            var snsConfig = new AmazonSimpleNotificationServiceConfig();

            if (!string.IsNullOrEmpty(connectionSettings.ServiceUrl))
            {
                sqsConfig.ServiceURL = connectionSettings.ServiceUrl;
                snsConfig.ServiceURL = connectionSettings.ServiceUrl;

                sqsConfig.AuthenticationRegion = connectionSettings.Region;
                snsConfig.AuthenticationRegion = connectionSettings.Region;
            }
            else
            {
                var regionEndpoint = RegionEndpoint.GetBySystemName(connectionSettings.Region);

                sqsConfig.RegionEndpoint = regionEndpoint;
                snsConfig.RegionEndpoint = regionEndpoint;
            }

            // always the IAM user of the connection string, never the credentials of the machine

            var credentials = new BasicAWSCredentials(connectionSettings.AccessKeyId, connectionSettings.SecretAccessKey);

            _sqsClient = new AmazonSQSClient(credentials, sqsConfig);
            _snsClient = new AmazonSimpleNotificationServiceClient(credentials, snsConfig);
        }

        public async Task<string> GetQueueUrlAsync(string queueName, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _sqsClient.GetQueueUrlAsync(new GetQueueUrlRequest(queueName), cancellationToken).ConfigureAwait(false);

                return response.QueueUrl;
            }
            catch (QueueDoesNotExistException)
            {
                return null;
            }
            catch (AmazonSQSException amazonSqsException) when (string.Equals(amazonSqsException.ErrorCode, "AWS.SimpleQueueService.NonExistentQueue"))
            {
                return null;
            }
        }

        public async Task<string> CreateQueueAsync(string queueName, Dictionary<string, string> attributes, CancellationToken cancellationToken)
        {
            var response = await _sqsClient.CreateQueueAsync(new CreateQueueRequest(queueName) { Attributes = attributes }, cancellationToken).ConfigureAwait(false);

            return response.QueueUrl;
        }

        public async Task<Dictionary<string, string>> GetQueueAttributesAsync(string queueUrl, List<string> attributeNames, CancellationToken cancellationToken)
        {
            var response = await _sqsClient.GetQueueAttributesAsync(new GetQueueAttributesRequest(queueUrl, attributeNames), cancellationToken).ConfigureAwait(false);

            return response.Attributes ?? [];
        }

        public async Task SetQueueAttributesAsync(string queueUrl, Dictionary<string, string> attributes, CancellationToken cancellationToken)
        {
            await _sqsClient.SetQueueAttributesAsync(new SetQueueAttributesRequest(queueUrl, attributes), cancellationToken).ConfigureAwait(false);
        }

        public async Task<List<Amazon.SQS.Model.Message>> ReceiveMessagesAsync(ReceiveMessageRequest request, CancellationToken cancellationToken)
        {
            var response = await _sqsClient.ReceiveMessageAsync(request, cancellationToken).ConfigureAwait(false);

            return response.Messages ?? [];
        }

        public async Task SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken)
        {
            await _sqsClient.SendMessageAsync(request, cancellationToken).ConfigureAwait(false);
        }

        public async Task<List<SqsBatchResultErrorEntry>> SendMessageBatchAsync(SendMessageBatchRequest request, CancellationToken cancellationToken)
        {
            var response = await _sqsClient.SendMessageBatchAsync(request, cancellationToken).ConfigureAwait(false);

            return response.Failed ?? [];
        }

        public async Task<List<SqsBatchResultErrorEntry>> DeleteMessageBatchAsync(DeleteMessageBatchRequest request, CancellationToken cancellationToken)
        {
            var response = await _sqsClient.DeleteMessageBatchAsync(request, cancellationToken).ConfigureAwait(false);

            return response.Failed ?? [];
        }

        public async Task<List<SqsBatchResultErrorEntry>> ChangeMessageVisibilityBatchAsync(ChangeMessageVisibilityBatchRequest request, CancellationToken cancellationToken)
        {
            var response = await _sqsClient.ChangeMessageVisibilityBatchAsync(request, cancellationToken).ConfigureAwait(false);

            return response.Failed ?? [];
        }

        public async Task<string> CreateTopicAsync(string topicName, CancellationToken cancellationToken)
        {
            var response = await _snsClient.CreateTopicAsync(new CreateTopicRequest(topicName), cancellationToken).ConfigureAwait(false);

            return response.TopicArn;
        }

        public async Task PublishAsync(PublishRequest request, CancellationToken cancellationToken)
        {
            await _snsClient.PublishAsync(request, cancellationToken).ConfigureAwait(false);
        }

        public async Task SubscribeQueueAsync(string topicArn, string queueArn, CancellationToken cancellationToken)
        {
            await _snsClient.SubscribeAsync(new SubscribeRequest(topicArn, "sqs", queueArn)
            {
                Attributes = new Dictionary<string, string>
                {
                    ["RawMessageDelivery"] = "true"
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _sqsClient.Dispose();
            _snsClient.Dispose();
        }
    }
}
