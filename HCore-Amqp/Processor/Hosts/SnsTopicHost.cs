using System;
using System.Text;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;
using HCore.Amqp.Message;
using HCore.Amqp.Messenger.Impl;
using HCore.Amqp.Sqs;
using Newtonsoft.Json;

namespace HCore.Amqp.Processor.Hosts
{
    // A topic is an SNS topic. Every instance receives it through its own SQS queue subscribed to the topic,
    // like the Service Bus subscription per machine.

    internal class SnsTopicHost
    {
        private readonly string _address;
        private readonly string _topicName;
        private readonly string _queueName;
        private readonly int _listenersCount;

        private readonly SqsMessengerImpl _messenger;

        private SqsQueueHost _queueHost;

        internal string TopicArn { get; private set; }

        internal SnsTopicHost(string address, string topicName, string queueName, int listenersCount, SqsMessengerImpl messenger)
        {
            _address = address;
            _topicName = topicName;
            _queueName = queueName;
            _listenersCount = listenersCount;

            _messenger = messenger;
        }

        // without CreateResources the topic ARN is built from its name, so sending only needs the permission to publish

        internal async Task InitializeAsync(string accountId)
        {
            if (_messenger.CreateResources)
            {
                Console.WriteLine($"Creating SNS topic {_topicName}...");

                TopicArn = await _messenger.SnsApi.CreateTopicAsync(_topicName, _messenger.CancellationToken).ConfigureAwait(false);

                Console.WriteLine($"Created SNS topic {_topicName}");
            }
            else
            {
                TopicArn = SqsNames.GetTopicArn(_messenger.Region, accountId, _topicName);
            }

            if (_listenersCount > 0)
            {
                _queueHost = new SqsQueueHost(_address, _queueName, isFifo: false, _listenersCount, maxReceiveCount: null, TopicArn, _messenger);

                await _queueHost.InitializeAsync().ConfigureAwait(false);
            }
        }

        internal void StartListeners()
        {
            _queueHost?.StartListeners();
        }

        internal async Task CancelReceiversAsync()
        {
            if (_queueHost != null)
            {
                await _queueHost.CancelReceiversAsync().ConfigureAwait(false);
            }
        }

        internal async Task SendMessageAsync(AMQPMessage body, double? timeOffsetSeconds)
        {
            // the topic queues keep a message for 60 s, so a later due time would never be reached

            if (timeOffsetSeconds >= SqsQueueHost.TopicMessageRetentionSeconds)
            {
                throw new Exception($"AMQP topic message for address {_address} can not be delayed by {SqsQueueHost.TopicMessageRetentionSeconds} s or more");
            }

            var messageBody = JsonConvert.SerializeObject(body);

            if (Encoding.UTF8.GetByteCount(messageBody) > SqsQueueHost.MaximumMessageSize)
            {
                throw new Exception($"AMQP message for address {_address} is larger than {SqsQueueHost.MaximumMessageSize} bytes");
            }

            var request = new PublishRequest(TopicArn, messageBody);

            // raw message delivery passes the attribute on to the subscribed queues, whose listeners hold the message until it is due

            if (timeOffsetSeconds > 0)
            {
                request.MessageAttributes[SqsQueueHost.NotBeforeAttributeName] = new MessageAttributeValue
                {
                    DataType = "Number",
                    StringValue = SqsQueueHost.GetNotBeforeValue(timeOffsetSeconds.Value)
                };
            }

            await _messenger.SendWithRetryAsync(_address, cancellationToken => _messenger.SnsApi.PublishAsync(request, cancellationToken)).ConfigureAwait(false);
        }
    }
}
