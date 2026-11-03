using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using HCore.Amqp.Sqs;
using Newtonsoft.Json;
using SnsMessageAttributeValue = Amazon.SimpleNotificationService.Model.MessageAttributeValue;
using SqsBatchResultErrorEntry = Amazon.SQS.Model.BatchResultErrorEntry;
using SqsMessage = Amazon.SQS.Model.Message;
using SqsMessageAttributeValue = Amazon.SQS.Model.MessageAttributeValue;

namespace HCore.Test.Amqp.Sqs
{
    // An in-memory SQS and SNS with the semantics the messenger relies on: visibility timeouts, receipt handles,
    // FIFO message groups (no message of a group while another one is in flight), deduplication, delays, redrive,
    // and SNS fan-out to subscribed queues whose policy allows the topic. It is strict where SQS is strict, so a
    // request SQS would reject fails here too.

    internal class FakeAws : ISqsApi, ISnsApi
    {
        internal const string AccountId = "123456789012";
        internal const string Region = "eu-central-1";

        private const int _maximumMessageSize = 256 * 1024;
        private const int _maximumVisibilitySeconds = 12 * 60 * 60;

        private readonly object _lock = new();

        private readonly Dictionary<string, FakeQueue> _queuesByName = [];
        private readonly Dictionary<string, List<FakeSubscription>> _subscriptionsByTopicArn = [];

        private int _receiveCalls;
        private int _failingSendCount;

        internal int ReceiveCalls => _receiveCalls;

        // the next n SendMessage calls fail before anything is stored

        internal int FailingSendCount
        {
            set => Interlocked.Exchange(ref _failingSendCount, value);
        }

        // batch entries this returns true for fail; the other entries of the batch are stored

        internal Func<SendMessageBatchRequestEntry, bool> FailBatchEntry { get; set; }

        // measured on SQS: a FIFO queue rejects a visibility change within about 250 ms of the previous change of the same
        // message with RequestThrottled, also when the message was received again in between; standard queues do not

        internal TimeSpan FifoVisibilityThrottleWindow { get; set; } = TimeSpan.FromMilliseconds(250);

        private int _throttledVisibilityChanges;

        internal int ThrottledVisibilityChanges => _throttledVisibilityChanges;

        internal List<SendMessageRequest> SentRequests { get; } = [];
        internal List<SendMessageBatchRequest> SentBatchRequests { get; } = [];
        internal List<string> CreatedQueueNames { get; } = [];
        internal List<string> CreatedTopicNames { get; } = [];

        internal static string GetQueueUrl(string queueName) => $"https://sqs.{Region}.amazonaws.com/{AccountId}/{queueName}";

        internal static string GetQueueArn(string queueName) => $"arn:aws:sqs:{Region}:{AccountId}:{queueName}";

        internal static string GetTopicArn(string topicName) => $"arn:aws:sns:{Region}:{AccountId}:{topicName}";

        // ---- test helpers ----

        internal void AddQueue(string queueName, Dictionary<string, string> attributes = null)
        {
            lock (_lock)
            {
                AddQueueInternally(queueName, attributes ?? (queueName.EndsWith(".fifo") ? new Dictionary<string, string> { ["FifoQueue"] = "true" } : []));
            }
        }

        internal void AddTopic(string topicName)
        {
            lock (_lock)
            {
                _subscriptionsByTopicArn.TryAdd(GetTopicArn(topicName), []);
            }
        }

        internal void Subscribe(string topicName, string queueName, bool rawMessageDelivery)
        {
            lock (_lock)
            {
                _subscriptionsByTopicArn[GetTopicArn(topicName)].Add(new FakeSubscription(GetQueueArn(queueName), rawMessageDelivery));
            }
        }

        // allows a topic to send to a queue, as Terraform would with the queue policy

        internal void AllowTopic(string queueName, string topicName)
        {
            lock (_lock)
            {
                _queuesByName[queueName].Attributes["Policy"] = $"{{\"aws:SourceArn\":\"{GetTopicArn(topicName)}\"}}";
            }
        }

        internal void RemoveQueue(string queueName)
        {
            lock (_lock)
            {
                _queuesByName.Remove(queueName);
            }
        }

        internal bool HasQueue(string queueName)
        {
            lock (_lock)
            {
                return _queuesByName.ContainsKey(queueName);
            }
        }

        internal Dictionary<string, string> GetAttributes(string queueName)
        {
            lock (_lock)
            {
                return new Dictionary<string, string>(_queuesByName[queueName].Attributes);
            }
        }

        internal List<FakeSubscription> GetSubscriptions(string topicName)
        {
            lock (_lock)
            {
                return _subscriptionsByTopicArn.TryGetValue(GetTopicArn(topicName), out var subscriptions) ? [.. subscriptions] : [];
            }
        }

        internal List<FakeMessageSnapshot> GetMessages(string queueName)
        {
            lock (_lock)
            {
                var now = DateTimeOffset.UtcNow;

                return _queuesByName[queueName].Messages
                    .Select(message => new FakeMessageSnapshot(message.Body, message.GroupId, message.ReceiveCount, message.IsInFlight(now), message.MessageAttributes))
                    .ToList();
            }
        }

        // ---- SQS ----

        public Task<string> GetQueueUrlAsync(string queueName, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                return Task.FromResult(_queuesByName.ContainsKey(queueName) ? GetQueueUrl(queueName) : null);
            }
        }

        public Task<string> CreateQueueAsync(string queueName, Dictionary<string, string> attributes, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                if (_queuesByName.TryGetValue(queueName, out var existingQueue))
                {
                    if (attributes.Any(attribute => !existingQueue.Attributes.TryGetValue(attribute.Key, out var value) || value != attribute.Value))
                    {
                        throw new QueueNameExistsException($"A queue named {queueName} already exists with different attributes");
                    }

                    return Task.FromResult(existingQueue.Url);
                }

                CreatedQueueNames.Add(queueName);

                return Task.FromResult(AddQueueInternally(queueName, attributes).Url);
            }
        }

        private FakeQueue AddQueueInternally(string queueName, Dictionary<string, string> attributes)
        {
            var isFifo = attributes.TryGetValue("FifoQueue", out var fifoQueue) && fifoQueue == "true";

            if (isFifo != queueName.EndsWith(".fifo"))
            {
                throw new AmazonSQSException($"Queue {queueName}: a FIFO queue name must end with .fifo and set FifoQueue");
            }

            var queue = new FakeQueue(queueName, isFifo, new Dictionary<string, string>(attributes));

            _queuesByName.Add(queueName, queue);

            return queue;
        }

        public Task<Dictionary<string, string>> GetQueueAttributesAsync(string queueUrl, List<string> attributeNames, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                var queue = GetQueueByUrl(queueUrl);

                var attributes = new Dictionary<string, string>();

                foreach (var attributeName in attributeNames)
                {
                    if (attributeName == "QueueArn")
                    {
                        attributes[attributeName] = queue.Arn;
                    }
                    else if (queue.Attributes.TryGetValue(attributeName, out var value))
                    {
                        attributes[attributeName] = value;
                    }
                }

                return Task.FromResult(attributes);
            }
        }

        public Task SetQueueAttributesAsync(string queueUrl, Dictionary<string, string> attributes, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                var queue = GetQueueByUrl(queueUrl);

                foreach (var attribute in attributes)
                {
                    queue.Attributes[attribute.Key] = attribute.Value;
                }

                return Task.CompletedTask;
            }
        }

        public async Task<List<SqsMessage>> ReceiveMessagesAsync(ReceiveMessageRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _receiveCalls);

            if (request.MaxNumberOfMessages < 1 || request.MaxNumberOfMessages > 10)
            {
                throw new AmazonSQSException($"MaxNumberOfMessages {request.MaxNumberOfMessages} is invalid");
            }

            var deadline = DateTimeOffset.UtcNow.AddSeconds(request.WaitTimeSeconds);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var messages = TryReceive(request);

                if (messages.Count > 0 || DateTimeOffset.UtcNow >= deadline)
                {
                    return messages;
                }

                await Task.Delay(20, cancellationToken).ConfigureAwait(false);
            }
        }

        private List<SqsMessage> TryReceive(ReceiveMessageRequest request)
        {
            lock (_lock)
            {
                var queue = GetQueueByUrl(request.QueueUrl);

                var now = DateTimeOffset.UtcNow;

                var candidates = new List<FakeMessage>();

                if (queue.IsFifo)
                {
                    // a group whose oldest message is in flight or not yet visible delivers nothing, so the order holds

                    var blockedGroupIds = new HashSet<string>();

                    foreach (var message in queue.Messages)
                    {
                        if (candidates.Count == request.MaxNumberOfMessages)
                        {
                            break;
                        }

                        if (blockedGroupIds.Contains(message.GroupId))
                        {
                            continue;
                        }

                        if (message.VisibleAt > now)
                        {
                            blockedGroupIds.Add(message.GroupId);

                            continue;
                        }

                        candidates.Add(message);
                    }
                }
                else
                {
                    candidates.AddRange(queue.Messages.Where(message => message.VisibleAt <= now).Take(request.MaxNumberOfMessages));
                }

                var visibilityTimeoutSeconds = request.VisibilityTimeout > 0
                    ? request.VisibilityTimeout
                    : int.Parse(queue.Attributes.GetValueOrDefault("VisibilityTimeout", "30"));

                var receivedMessages = new List<SqsMessage>();

                foreach (var message in candidates)
                {
                    message.ReceiveCount++;

                    if (queue.MaxReceiveCount.HasValue && message.ReceiveCount > queue.MaxReceiveCount.Value)
                    {
                        // moved to the dead-letter queue instead of being delivered

                        queue.Messages.Remove(message);

                        message.ReceiptHandle = null;
                        message.VisibleAt = now;

                        _queuesByName.Values.Single(deadLetterQueue => deadLetterQueue.Arn == queue.DeadLetterTargetArn).Messages.Add(message);

                        continue;
                    }

                    message.ReceiptHandle = Guid.NewGuid().ToString("N");
                    message.ReceivedAt = now;
                    message.VisibleAt = now.AddSeconds(visibilityTimeoutSeconds);

                    var attributes = new Dictionary<string, string>
                    {
                        ["ApproximateReceiveCount"] = $"{message.ReceiveCount}"
                    };

                    if (queue.IsFifo && request.MessageSystemAttributeNames.Contains("MessageGroupId"))
                    {
                        attributes["MessageGroupId"] = message.GroupId;
                    }

                    receivedMessages.Add(new SqsMessage
                    {
                        MessageId = message.MessageId,
                        ReceiptHandle = message.ReceiptHandle,
                        Body = message.Body,
                        Attributes = attributes,
                        MessageAttributes = message.MessageAttributes
                            .Where(messageAttribute => request.MessageAttributeNames.Contains(messageAttribute.Key) || request.MessageAttributeNames.Contains("All"))
                            .ToDictionary(messageAttribute => messageAttribute.Key, messageAttribute => messageAttribute.Value)
                    });
                }

                return receivedMessages;
            }
        }

        public Task SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                if (_failingSendCount > 0)
                {
                    _failingSendCount--;

                    throw new AmazonSQSException("Injected send failure");
                }

                SentRequests.Add(request);

                Enqueue(GetQueueByUrl(request.QueueUrl), request.MessageBody, request.MessageGroupId, request.MessageDeduplicationId, request.DelaySeconds, request.MessageAttributes);

                return Task.CompletedTask;
            }
        }

        public Task<List<SqsBatchResultErrorEntry>> SendMessageBatchAsync(SendMessageBatchRequest request, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                if (request.Entries.Count > 10)
                {
                    throw new AmazonSQSException("TooManyEntriesInBatchRequest");
                }

                if (request.Entries.Sum(entry => Encoding.UTF8.GetByteCount(entry.MessageBody)) > _maximumMessageSize)
                {
                    throw new AmazonSQSException("BatchRequestTooLong");
                }

                SentBatchRequests.Add(request);

                var queue = GetQueueByUrl(request.QueueUrl);

                var failedEntries = new List<SqsBatchResultErrorEntry>();

                foreach (var entry in request.Entries)
                {
                    if (FailBatchEntry != null && FailBatchEntry(entry))
                    {
                        failedEntries.Add(new SqsBatchResultErrorEntry { Id = entry.Id, Code = "InternalError", Message = "Injected batch entry failure", SenderFault = false });

                        continue;
                    }

                    Enqueue(queue, entry.MessageBody, entry.MessageGroupId, entry.MessageDeduplicationId, entry.DelaySeconds, entry.MessageAttributes);
                }

                return Task.FromResult(failedEntries);
            }
        }

        private static void Enqueue(FakeQueue queue, string body, string groupId, string deduplicationId, int delaySeconds, Dictionary<string, SqsMessageAttributeValue> messageAttributes)
        {
            if (Encoding.UTF8.GetByteCount(body) > _maximumMessageSize)
            {
                throw new AmazonSQSException("Message too long");
            }

            if (messageAttributes != null && messageAttributes.Count > 10)
            {
                throw new AmazonSQSException("Too many message attributes");
            }

            var now = DateTimeOffset.UtcNow;

            if (queue.IsFifo)
            {
                if (string.IsNullOrEmpty(groupId) || groupId.Length > 128 || !groupId.All(character => character >= '!' && character <= '~'))
                {
                    throw new AmazonSQSException($"MessageGroupId is missing or invalid: {groupId}");
                }

                if (string.IsNullOrEmpty(deduplicationId))
                {
                    throw new AmazonSQSException("MessageDeduplicationId is missing and content-based deduplication is off");
                }

                if (delaySeconds > 0)
                {
                    throw new AmazonSQSException("DelaySeconds per message is not valid for FIFO queues");
                }

                var deduplicationKey = $"{groupId}|{deduplicationId}";

                if (queue.DeduplicatedAtByKey.TryGetValue(deduplicationKey, out var deduplicatedAt) && now - deduplicatedAt < TimeSpan.FromMinutes(5))
                {
                    return;
                }

                queue.DeduplicatedAtByKey[deduplicationKey] = now;
            }
            else
            {
                if (!string.IsNullOrEmpty(groupId))
                {
                    throw new AmazonSQSException("MessageGroupId is not used on standard queues by the messenger");
                }

                if (delaySeconds < 0 || delaySeconds > 900)
                {
                    throw new AmazonSQSException($"DelaySeconds {delaySeconds} is invalid");
                }
            }

            queue.Messages.Add(new FakeMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                Body = body,
                GroupId = groupId,
                MessageAttributes = messageAttributes != null ? new Dictionary<string, SqsMessageAttributeValue>(messageAttributes) : [],
                VisibleAt = now.AddSeconds(delaySeconds)
            });
        }

        public Task<List<SqsBatchResultErrorEntry>> DeleteMessageBatchAsync(DeleteMessageBatchRequest request, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                if (request.Entries.Count > 10)
                {
                    throw new AmazonSQSException("TooManyEntriesInBatchRequest");
                }

                var queue = GetQueueByUrl(request.QueueUrl);

                var failedEntries = new List<SqsBatchResultErrorEntry>();

                foreach (var entry in request.Entries)
                {
                    var message = queue.Messages.SingleOrDefault(message => message.ReceiptHandle == entry.ReceiptHandle);

                    if (message == null)
                    {
                        failedEntries.Add(new SqsBatchResultErrorEntry { Id = entry.Id, Code = "ReceiptHandleIsInvalid", Message = "Stale receipt handle", SenderFault = true });

                        continue;
                    }

                    queue.Messages.Remove(message);
                }

                return Task.FromResult(failedEntries);
            }
        }

        public Task<List<SqsBatchResultErrorEntry>> ChangeMessageVisibilityBatchAsync(ChangeMessageVisibilityBatchRequest request, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                if (request.Entries.Count > 10)
                {
                    throw new AmazonSQSException("TooManyEntriesInBatchRequest");
                }

                var queue = GetQueueByUrl(request.QueueUrl);

                var now = DateTimeOffset.UtcNow;

                var failedEntries = new List<SqsBatchResultErrorEntry>();

                foreach (var entry in request.Entries)
                {
                    var message = queue.Messages.SingleOrDefault(message => message.ReceiptHandle == entry.ReceiptHandle);

                    if (message == null || !message.IsInFlight(now))
                    {
                        failedEntries.Add(new SqsBatchResultErrorEntry { Id = entry.Id, Code = "MessageNotInflight", Message = "Message is not in flight", SenderFault = true });

                        continue;
                    }

                    if (entry.VisibilityTimeout < 0 || (now - message.ReceivedAt).TotalSeconds + entry.VisibilityTimeout > _maximumVisibilitySeconds)
                    {
                        failedEntries.Add(new SqsBatchResultErrorEntry { Id = entry.Id, Code = "InvalidParameterValue", Message = "Visibility beyond 12 hours", SenderFault = true });

                        continue;
                    }

                    if (queue.IsFifo && message.VisibilityChangedAt.HasValue && now - message.VisibilityChangedAt.Value < FifoVisibilityThrottleWindow)
                    {
                        Interlocked.Increment(ref _throttledVisibilityChanges);

                        failedEntries.Add(new SqsBatchResultErrorEntry { Id = entry.Id, Code = "RequestThrottled", Message = "Request throttled because message visibility configuration was updated too recently", SenderFault = true });

                        continue;
                    }

                    message.VisibleAt = now.AddSeconds(entry.VisibilityTimeout);
                    message.VisibilityChangedAt = now;
                }

                return Task.FromResult(failedEntries);
            }
        }

        private FakeQueue GetQueueByUrl(string queueUrl)
        {
            var queueName = queueUrl[(queueUrl.LastIndexOf('/') + 1)..];

            if (!_queuesByName.TryGetValue(queueName, out var queue))
            {
                throw new QueueDoesNotExistException($"Queue {queueName} does not exist");
            }

            return queue;
        }

        // ---- SNS ----

        public Task<string> CreateTopicAsync(string topicName, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                var topicArn = GetTopicArn(topicName);

                if (_subscriptionsByTopicArn.TryAdd(topicArn, []))
                {
                    CreatedTopicNames.Add(topicName);
                }

                return Task.FromResult(topicArn);
            }
        }

        public Task PublishAsync(PublishRequest request, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                if (!_subscriptionsByTopicArn.TryGetValue(request.TopicArn, out var subscriptions))
                {
                    throw new NotFoundException($"Topic {request.TopicArn} does not exist");
                }

                foreach (var subscription in subscriptions)
                {
                    var queue = _queuesByName.Values.SingleOrDefault(queue => queue.Arn == subscription.QueueArn);

                    // SNS can only deliver to a queue whose policy allows the topic

                    if (queue == null || !queue.Attributes.TryGetValue("Policy", out var policy) || !policy.Contains(request.TopicArn))
                    {
                        continue;
                    }

                    if (subscription.RawMessageDelivery)
                    {
                        var messageAttributes = request.MessageAttributes.ToDictionary(
                            messageAttribute => messageAttribute.Key,
                            messageAttribute => new SqsMessageAttributeValue { DataType = messageAttribute.Value.DataType, StringValue = messageAttribute.Value.StringValue });

                        Enqueue(queue, request.Message, null, null, 0, messageAttributes);
                    }
                    else
                    {
                        var envelope = JsonConvert.SerializeObject(new
                        {
                            Type = "Notification",
                            MessageId = Guid.NewGuid().ToString(),
                            request.TopicArn,
                            request.Message,
                            MessageAttributes = request.MessageAttributes.ToDictionary(
                                messageAttribute => messageAttribute.Key,
                                messageAttribute => new { Type = messageAttribute.Value.DataType, Value = messageAttribute.Value.StringValue })
                        });

                        Enqueue(queue, envelope, null, null, 0, null);
                    }
                }

                return Task.CompletedTask;
            }
        }

        public Task SubscribeQueueAsync(string topicArn, string queueArn, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                if (!_subscriptionsByTopicArn.TryGetValue(topicArn, out var subscriptions))
                {
                    throw new NotFoundException($"Topic {topicArn} does not exist");
                }

                if (!subscriptions.Any(subscription => subscription.QueueArn == queueArn))
                {
                    subscriptions.Add(new FakeSubscription(queueArn, RawMessageDelivery: true));
                }

                return Task.CompletedTask;
            }
        }

        private class FakeQueue
        {
            internal FakeQueue(string name, bool isFifo, Dictionary<string, string> attributes)
            {
                Name = name;
                IsFifo = isFifo;
                Attributes = attributes;

                if (attributes.TryGetValue("RedrivePolicy", out var redrivePolicy))
                {
                    var redrive = JsonConvert.DeserializeAnonymousType(redrivePolicy, new { deadLetterTargetArn = "", maxReceiveCount = 0 });

                    DeadLetterTargetArn = redrive.deadLetterTargetArn;
                    MaxReceiveCount = redrive.maxReceiveCount;
                }
            }

            internal string Name { get; }
            internal bool IsFifo { get; }
            internal Dictionary<string, string> Attributes { get; }

            internal string Url => GetQueueUrl(Name);
            internal string Arn => GetQueueArn(Name);

            internal string DeadLetterTargetArn { get; }
            internal int? MaxReceiveCount { get; }

            internal List<FakeMessage> Messages { get; } = [];

            internal Dictionary<string, DateTimeOffset> DeduplicatedAtByKey { get; } = [];
        }

        private class FakeMessage
        {
            internal string MessageId { get; init; }
            internal string Body { get; init; }
            internal string GroupId { get; init; }
            internal Dictionary<string, SqsMessageAttributeValue> MessageAttributes { get; init; }

            internal DateTimeOffset VisibleAt { get; set; }
            internal DateTimeOffset ReceivedAt { get; set; }
            internal int ReceiveCount { get; set; }
            internal string ReceiptHandle { get; set; }
            internal DateTimeOffset? VisibilityChangedAt { get; set; }

            internal bool IsInFlight(DateTimeOffset now) => ReceiptHandle != null && VisibleAt > now;
        }
    }

    internal record FakeSubscription(string QueueArn, bool RawMessageDelivery);

    internal record FakeMessageSnapshot(string Body, string GroupId, int ReceiveCount, bool IsInFlight, Dictionary<string, SqsMessageAttributeValue> MessageAttributes);
}
