using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using HCore.Amqp.Exceptions;
using HCore.Amqp.Message;
using HCore.Amqp.Messenger.Impl;
using HCore.Amqp.Sqs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HCore.Amqp.Processor.Hosts
{
    // A FIFO queue keeps the order within a message group (the session ID): while a message of a group is invisible,
    // SQS delivers no other message of that group, but the other groups go to any free listener. Nothing binds a group
    // to a listener, so a failing group only blocks itself.

    internal class SqsQueueHost
    {
        // a delayed message carries the time it is due; FIFO queues have no delay per message

        internal const string NotBeforeAttributeName = "HCoreNotBefore";

        // the SNS limit and the SQS default maximum message size

        internal const int MaximumMessageSize = 256 * 1024;

        private const string _messageGroupIdAttributeName = "MessageGroupId";

        private const int _maximumBatchSize = 10;
        private const int _maximumDelaySeconds = 900;
        private const int _maximumMessageGroupIdLength = 128;

        private const int _messageRetentionSeconds = 14 * 24 * 60 * 60;

        private const string _requestThrottledCode = "RequestThrottled";
        private const int _maximumThrottleAttempts = 5;

        // the same as the message time to live of the Service Bus topics

        internal const int TopicMessageRetentionSeconds = 60;

        private readonly string _address;
        private readonly string _queueName;
        private readonly bool _isFifo;
        private readonly int _listenersCount;
        private readonly int? _maxReceiveCount;

        // set for the queue through which this instance receives a topic

        private readonly string _topicArn;

        private readonly SqsMessengerImpl _messenger;
        private readonly ISqsApi _sqsApi;
        private readonly ISnsApi _snsApi;
        private readonly SqsTimings _timings;

        private readonly CancellationToken _cancellationToken;
        private readonly CancellationTokenSource _receiversCancellationTokenSource;

        private readonly ILogger<SqsMessengerImpl> _logger;

        internal string QueueUrl { get; private set; }

        internal SqsQueueHost(string address, string queueName, bool isFifo, int listenersCount, int? maxReceiveCount, string topicArn, SqsMessengerImpl messenger)
        {
            _address = address;
            _queueName = queueName;
            _isFifo = isFifo;
            _listenersCount = listenersCount;
            _maxReceiveCount = maxReceiveCount;
            _topicArn = topicArn;

            _messenger = messenger;
            _sqsApi = messenger.SqsApi;
            _snsApi = messenger.SnsApi;
            _timings = messenger.Timings;

            _cancellationToken = messenger.CancellationToken;
            _receiversCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);

            _logger = messenger.Logger;
        }

        internal async Task InitializeAsync()
        {
            QueueUrl = await _sqsApi.GetQueueUrlAsync(_queueName, _cancellationToken).ConfigureAwait(false);

            if (QueueUrl == null)
            {
                if (!_messenger.CreateResources)
                {
                    throw new Exception($"SQS queue {_queueName} for address {_address} does not exist");
                }

                Console.WriteLine($"Creating SQS queue {_queueName}...");

                QueueUrl = await CreateQueueAsync().ConfigureAwait(false);

                Console.WriteLine($"Created SQS queue {_queueName}");
            }

            if (_topicArn != null && _messenger.CreateResources)
            {
                await SubscribeToTopicAsync().ConfigureAwait(false);
            }
        }

        private async Task<string> CreateQueueAsync()
        {
            var attributes = GetQueueAttributes(_isFifo, _topicArn != null ? TopicMessageRetentionSeconds : _messageRetentionSeconds);

            if (_maxReceiveCount.HasValue)
            {
                var deadLetterQueueName = SqsNames.GetDeadLetterQueueName(_queueName);

                var deadLetterQueueUrl = await GetOrCreateQueueAsync(deadLetterQueueName, GetQueueAttributes(_isFifo, _messageRetentionSeconds)).ConfigureAwait(false);
                var deadLetterQueueArn = await GetQueueArnAsync(deadLetterQueueUrl).ConfigureAwait(false);

                attributes["RedrivePolicy"] = JsonConvert.SerializeObject(new
                {
                    deadLetterTargetArn = deadLetterQueueArn,
                    maxReceiveCount = _maxReceiveCount.Value
                });
            }

            return await GetOrCreateQueueAsync(_queueName, attributes).ConfigureAwait(false);
        }

        private Dictionary<string, string> GetQueueAttributes(bool isFifo, int messageRetentionSeconds)
        {
            var attributes = new Dictionary<string, string>
            {
                ["VisibilityTimeout"] = $"{(int)_timings.VisibilityTimeout.TotalSeconds}",
                ["MessageRetentionPeriod"] = $"{messageRetentionSeconds}",
                ["ReceiveMessageWaitTimeSeconds"] = $"{_timings.ReceiveWaitTimeSeconds}",
                ["SqsManagedSseEnabled"] = "true"
            };

            if (isFifo)
            {
                // the deduplication ID is a new GUID per message: the sync chain sends identical bodies on purpose

                attributes["FifoQueue"] = "true";
                attributes["ContentBasedDeduplication"] = "false";
                attributes["DeduplicationScope"] = "messageGroup";
                attributes["FifoThroughputLimit"] = "perMessageGroupId";
            }

            return attributes;
        }

        // several instances may start at once; an existing queue with other attributes is used as it is

        private async Task<string> GetOrCreateQueueAsync(string queueName, Dictionary<string, string> attributes)
        {
            try
            {
                return await _sqsApi.CreateQueueAsync(queueName, attributes, _cancellationToken).ConfigureAwait(false);
            }
            catch (QueueNameExistsException)
            {
                return await _sqsApi.GetQueueUrlAsync(queueName, _cancellationToken).ConfigureAwait(false)
                    ?? throw new Exception($"SQS queue {queueName} exists, but its URL cannot be read");
            }
        }

        private async Task<string> GetQueueArnAsync(string queueUrl)
        {
            var attributes = await _sqsApi.GetQueueAttributesAsync(queueUrl, ["QueueArn"], _cancellationToken).ConfigureAwait(false);

            if (!attributes.TryGetValue("QueueArn", out var queueArn) || string.IsNullOrEmpty(queueArn))
            {
                throw new Exception($"SQS queue ARN of {queueUrl} is missing");
            }

            return queueArn;
        }

        private async Task SubscribeToTopicAsync()
        {
            var queueArn = await GetQueueArnAsync(QueueUrl).ConfigureAwait(false);

            var policy = JsonConvert.SerializeObject(new
            {
                Version = "2012-10-17",
                Statement = new[]
                {
                    new
                    {
                        Effect = "Allow",
                        Principal = new { Service = "sns.amazonaws.com" },
                        Action = "sqs:SendMessage",
                        Resource = queueArn,
                        Condition = new { ArnEquals = new Dictionary<string, string> { ["aws:SourceArn"] = _topicArn } }
                    }
                }
            });

            await _sqsApi.SetQueueAttributesAsync(QueueUrl, new Dictionary<string, string> { ["Policy"] = policy }, _cancellationToken).ConfigureAwait(false);

            await _snsApi.SubscribeQueueAsync(_topicArn, queueArn, _cancellationToken).ConfigureAwait(false);
        }

        internal void StartListeners()
        {
            for (var i = 0; i < _listenersCount; i++)
            {
                _ = Task.Run(async () => await RunListenerAsync().ConfigureAwait(false));
            }
        }

        // messages being processed are still completed; WaitForTaskCompletionAsync waits for them

        internal async Task CancelReceiversAsync()
        {
            await _receiversCancellationTokenSource.CancelAsync().ConfigureAwait(false);
        }

        private async Task RunListenerAsync()
        {
            var receiversCancellationToken = _receiversCancellationTokenSource.Token;

            while (!receiversCancellationToken.IsCancellationRequested)
            {
                List<Amazon.SQS.Model.Message> messages;

                try
                {
                    messages = await _sqsApi.ReceiveMessagesAsync(new ReceiveMessageRequest(QueueUrl)
                    {
                        // a FIFO receive returns the waiting messages of a group together, like a Service Bus session

                        MaxNumberOfMessages = _isFifo ? _maximumBatchSize : 1,
                        WaitTimeSeconds = _timings.ReceiveWaitTimeSeconds,
                        VisibilityTimeout = (int)_timings.VisibilityTimeout.TotalSeconds,
                        MessageAttributeNames = [NotBeforeAttributeName],
                        MessageSystemAttributeNames = [_messageGroupIdAttributeName]
                    }, receiversCancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (receiversCancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    if (!receiversCancellationToken.IsCancellationRequested)
                    {
                        _logger.LogError($"SQS receive for address {_address} failed, retrying: {exception}");
                    }

                    await TryDelayAsync(_timings.RetryDelay, receiversCancellationToken).ConfigureAwait(false);

                    continue;
                }

                if (messages.Count == 0)
                {
                    continue;
                }

                try
                {
                    await ProcessReceivedMessagesAsync(messages).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    // the messages become visible again when their visibility timeout ends

                    _logger.LogError($"SQS listener for address {_address} failed to handle received messages: {exception}");
                }
            }
        }

        // A FIFO receive can return the messages of several groups. The listener processes every group it received, one after
        // the other and each as one batch, like a Service Bus session. It hands none of them back: it would receive them again
        // at once, and SQS throttles a visibility change that follows another one of the same message within a moment.

        private async Task ProcessReceivedMessagesAsync(List<Amazon.SQS.Model.Message> messages)
        {
            var receivedAt = DateTimeOffset.UtcNow;

            var receivedMessages = messages.Select(message => ToReceivedMessage(message, receivedAt)).ToList();

            List<List<ReceivedMessage>> batches = _isFifo
                ? receivedMessages.GroupBy(receivedMessage => receivedMessage.GroupId).Select(group => group.ToList()).ToList()
                : [receivedMessages];

            using var heartbeat = new Heartbeat(receivedMessages);
            using var heartbeatCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);

            var heartbeatTask = RunHeartbeatAsync(heartbeat, heartbeatCancellationTokenSource.Token);

            try
            {
                for (var batchIndex = 0; batchIndex < batches.Count; batchIndex++)
                {
                    if (_receiversCancellationTokenSource.IsCancellationRequested)
                    {
                        // shutting down: the groups not started yet go back to the queue for the other instances

                        var notStartedMessages = batches.Skip(batchIndex).SelectMany(batch => batch).ToList();

                        await heartbeat.RemoveAsync(notStartedMessages).ConfigureAwait(false);

                        await ChangeVisibilityAsync(notStartedMessages, TimeSpan.Zero).ConfigureAwait(false);

                        return;
                    }

                    var dueMessages = await HoldMessagesNotDueAsync(batches[batchIndex], heartbeat).ConfigureAwait(false);

                    if (dueMessages.Count > 0)
                    {
                        await ProcessDueMessagesAsync(dueMessages, heartbeat).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                await heartbeatCancellationTokenSource.CancelAsync().ConfigureAwait(false);

                await heartbeatTask.ConfigureAwait(false);
            }
        }

        // A delayed message that arrives early stays invisible until it is due. On a FIFO queue its group waits behind it,
        // so the messages after it go back to the queue and follow once it has been processed.

        private async Task<List<ReceivedMessage>> HoldMessagesNotDueAsync(List<ReceivedMessage> receivedMessages, Heartbeat heartbeat)
        {
            var now = DateTimeOffset.UtcNow;

            var firstNotDueIndex = receivedMessages.FindIndex(receivedMessage => receivedMessage.NotBefore > now);

            if (firstNotDueIndex < 0)
            {
                return receivedMessages;
            }

            var notDueMessage = receivedMessages[firstNotDueIndex];

            await heartbeat.RemoveAsync(receivedMessages.Skip(firstNotDueIndex)).ConfigureAwait(false);

            await ChangeVisibilityAsync([notDueMessage], notDueMessage.NotBefore.Value - now).ConfigureAwait(false);
            await ChangeVisibilityAsync(receivedMessages.Skip(firstNotDueIndex + 1).ToList(), TimeSpan.Zero).ConfigureAwait(false);

            return receivedMessages.Take(firstNotDueIndex).ToList();
        }

        private async Task ProcessDueMessagesAsync(List<ReceivedMessage> receivedMessages, Heartbeat heartbeat)
        {
            TimeSpan? visibilityTimeSpan = null;

            try
            {
                if (_isFifo)
                {
                    await _messenger.ProcessMessagesAsync(_address, receivedMessages.Select(receivedMessage => receivedMessage.Body).ToList()).ConfigureAwait(false);
                }
                else
                {
                    foreach (var receivedMessage in receivedMessages)
                    {
                        await _messenger.ProcessMessageAsync(_address, receivedMessage.Body).ConfigureAwait(false);
                    }
                }
            }
            catch (RescheduleException)
            {
                // no log, this is "wanted"

                visibilityTimeSpan = TimeSpan.Zero;
            }
            catch (PostponeException postponeException)
            {
                // intentionally holding the messages; on a FIFO queue this blocks only their group

                visibilityTimeSpan = postponeException.LockSessionTimeSpan ?? (_isFifo ? _timings.FifoPostponeHold : _timings.StandardPostponeHold);
            }
            catch (Exception exception)
            {
                _logger.LogError($"Exception during processing AMQP message(s), holding them before they are delivered again: {exception}");

                visibilityTimeSpan = _timings.ErrorHold;
            }

            // the heartbeat must not extend the visibility after it was set below

            await heartbeat.RemoveAsync(receivedMessages).ConfigureAwait(false);

            if (visibilityTimeSpan == null)
            {
                await DeleteAsync(receivedMessages).ConfigureAwait(false);
            }
            else
            {
                await ChangeVisibilityAsync(receivedMessages, visibilityTimeSpan.Value).ConfigureAwait(false);
            }
        }

        // keeps the messages being processed or waiting for their turn invisible, like the Service Bus lock renewal

        private async Task RunHeartbeatAsync(Heartbeat heartbeat, CancellationToken cancellationToken)
        {
            while (await TryDelayAsync(_timings.HeartbeatInterval, cancellationToken).ConfigureAwait(false))
            {
                var elapsedTimeSpan = DateTimeOffset.UtcNow - heartbeat.ReceivedAt;

                if (elapsedTimeSpan + _timings.VisibilityTimeout > _timings.MaximumVisibility)
                {
                    _logger.LogWarning($"SQS message processing for address {_address} takes longer than {_timings.MaximumVisibility}, the message(s) become visible again and may be processed twice");

                    return;
                }

                await heartbeat.ExtendAsync(pendingMessages => ChangeVisibilityAsync(pendingMessages, _timings.VisibilityTimeout)).ConfigureAwait(false);
            }
        }

        private async Task ChangeVisibilityAsync(List<ReceivedMessage> receivedMessages, TimeSpan visibilityTimeSpan)
        {
            foreach (var batch in receivedMessages.Chunk(_maximumBatchSize))
            {
                var pendingMessages = batch.ToList();

                for (var attempt = 1; pendingMessages.Count > 0; attempt++)
                {
                    var entries = pendingMessages.Select((receivedMessage, index) => new ChangeMessageVisibilityBatchRequestEntry($"{index}", receivedMessage.Message.ReceiptHandle)
                    {
                        VisibilityTimeout = GetVisibilityTimeoutSeconds(receivedMessage, visibilityTimeSpan)
                    }).ToList();

                    List<BatchResultErrorEntry> failedEntries;

                    try
                    {
                        failedEntries = await _sqsApi.ChangeMessageVisibilityBatchAsync(new ChangeMessageVisibilityBatchRequest(QueueUrl, entries), CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        if (!_cancellationToken.IsCancellationRequested)
                        {
                            _logger.LogWarning($"SQS visibility of messages on address {_address} could not be changed, they become visible when their current visibility timeout ends: {exception.Message}");
                        }

                        break;
                    }

                    // the throttled changes are sent again once the throttle window has passed, e.g. a final change that
                    // came right after the heartbeat extended the same messages

                    var throttledEntries = failedEntries.Where(failedEntry => string.Equals(failedEntry.Code, _requestThrottledCode)).ToList();

                    foreach (var failedEntry in failedEntries.Except(throttledEntries).Concat(attempt < _maximumThrottleAttempts ? [] : throttledEntries))
                    {
                        _logger.LogWarning($"SQS visibility of a message on address {_address} could not be changed, it becomes visible when its current visibility timeout ends: {failedEntry.Code} {failedEntry.Message}");
                    }

                    if (throttledEntries.Count == 0 || attempt >= _maximumThrottleAttempts)
                    {
                        break;
                    }

                    pendingMessages = throttledEntries.Select(throttledEntry => pendingMessages[int.Parse(throttledEntry.Id, CultureInfo.InvariantCulture)]).ToList();

                    await Task.Delay(_timings.ThrottleRetryDelay).ConfigureAwait(false);
                }
            }
        }

        // SQS rejects a visibility beyond 12 hours after the receive

        private int GetVisibilityTimeoutSeconds(ReceivedMessage receivedMessage, TimeSpan visibilityTimeSpan)
        {
            var remainingSeconds = (_timings.MaximumVisibility - (DateTimeOffset.UtcNow - receivedMessage.ReceivedAt)).TotalSeconds - 1;

            return (int)Math.Max(0, Math.Min(Math.Ceiling(visibilityTimeSpan.TotalSeconds), remainingSeconds));
        }

        private async Task DeleteAsync(List<ReceivedMessage> receivedMessages)
        {
            foreach (var batch in receivedMessages.Chunk(_maximumBatchSize))
            {
                var entries = batch.Select((receivedMessage, index) => new DeleteMessageBatchRequestEntry($"{index}", receivedMessage.Message.ReceiptHandle)).ToList();

                try
                {
                    var failedEntries = await _sqsApi.DeleteMessageBatchAsync(new DeleteMessageBatchRequest(QueueUrl, entries), CancellationToken.None).ConfigureAwait(false);

                    foreach (var failedEntry in failedEntries)
                    {
                        _logger.LogError($"SQS message on address {_address} could not be deleted after processing and may be processed again: {failedEntry.Code} {failedEntry.Message}");
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogError($"SQS messages on address {_address} could not be deleted after processing and may be processed again: {exception}");
                }
            }
        }

        private ReceivedMessage ToReceivedMessage(Amazon.SQS.Model.Message message, DateTimeOffset receivedAt)
        {
            var body = message.Body;

            string notBeforeValue = null;

            if (message.MessageAttributes != null && message.MessageAttributes.TryGetValue(NotBeforeAttributeName, out var notBeforeAttributeValue))
            {
                notBeforeValue = notBeforeAttributeValue.StringValue;
            }

            if (_topicArn != null)
            {
                (body, notBeforeValue) = UnwrapSnsEnvelope(body, notBeforeValue);
            }

            string groupId = null;

            message.Attributes?.TryGetValue(_messageGroupIdAttributeName, out groupId);

            DateTimeOffset? notBefore = long.TryParse(notBeforeValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var notBeforeMilliseconds)
                ? DateTimeOffset.FromUnixTimeMilliseconds(notBeforeMilliseconds)
                : null;

            return new ReceivedMessage(message, body, groupId, notBefore, receivedAt);
        }

        // a subscription without raw message delivery wraps the published body in an SNS notification

        private static (string, string) UnwrapSnsEnvelope(string body, string notBeforeValue)
        {
            if (string.IsNullOrEmpty(body) || !body.TrimStart().StartsWith('{'))
            {
                return (body, notBeforeValue);
            }

            try
            {
                var envelope = JObject.Parse(body);

                if (!string.Equals(envelope.Value<string>("Type"), "Notification") || envelope["TopicArn"] == null || envelope["Message"] == null)
                {
                    return (body, notBeforeValue);
                }

                var envelopeNotBeforeValue = envelope["MessageAttributes"]?[NotBeforeAttributeName]?.Value<string>("Value");

                return (envelope.Value<string>("Message"), envelopeNotBeforeValue ?? notBeforeValue);
            }
            catch (JsonException)
            {
                return (body, notBeforeValue);
            }
        }

        internal async Task SendMessageAsync(AMQPMessage body, double? timeOffsetSeconds, string sessionId)
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                if (!_isFifo)
                {
                    throw new Exception("SQS queue is no session queue");
                }
            }
            else if (_isFifo)
            {
                throw new Exception("Session ID is missing for SQS FIFO queue");
            }

            var messageBody = SerializeMessageBody(body);

            // built once, so a retry sends the same deduplication ID and SQS drops a copy that was already stored

            var request = new SendMessageRequest(QueueUrl, messageBody)
            {
                MessageAttributes = GetMessageAttributes(timeOffsetSeconds)
            };

            if (_isFifo)
            {
                request.MessageGroupId = GetMessageGroupId(sessionId);
                request.MessageDeduplicationId = Guid.NewGuid().ToString("N");
            }
            else if (timeOffsetSeconds > 0)
            {
                request.DelaySeconds = (int)Math.Min(Math.Ceiling(timeOffsetSeconds.Value), _maximumDelaySeconds);
            }

            await _messenger.SendWithRetryAsync(_address, cancellationToken => _sqsApi.SendMessageAsync(request, cancellationToken)).ConfigureAwait(false);
        }

        internal async Task SendMessagesAsync<T>(ICollection<T> bodies, double? timeOffsetSeconds, string sessionId) where T : AMQPMessage
        {
            if (timeOffsetSeconds.HasValue)
            {
                throw new NotImplementedException();
            }

            if (_isFifo || !string.IsNullOrEmpty(sessionId))
            {
                throw new NotImplementedException();
            }

            var messageBodies = bodies.Select(body => SerializeMessageBody(body)).ToList();

            foreach (var batch in GetSendBatches(messageBodies))
            {
                var entries = batch.Select((messageBody, index) => new SendMessageBatchRequestEntry($"{index}", messageBody)).ToList();

                await _messenger.SendWithRetryAsync(_address, async cancellationToken =>
                {
                    var failedEntries = await _sqsApi.SendMessageBatchAsync(new SendMessageBatchRequest(QueueUrl, entries), cancellationToken).ConfigureAwait(false);

                    if (failedEntries.Count > 0)
                    {
                        // the other entries are stored; only the failed ones are sent again

                        var failedEntryIds = failedEntries.Select(failedEntry => failedEntry.Id).ToHashSet();

                        entries = entries.Where(entry => failedEntryIds.Contains(entry.Id)).ToList();

                        throw new Exception($"SQS batch send failed for {failedEntries.Count} message(s): {string.Join(", ", failedEntries.Select(failedEntry => $"{failedEntry.Code} {failedEntry.Message}"))}");
                    }
                }).ConfigureAwait(false);
            }
        }

        // a batch holds up to 10 messages and up to the maximum message size in total

        private static List<List<string>> GetSendBatches(List<string> messageBodies)
        {
            var batches = new List<List<string>>();

            List<string> currentBatch = null;
            var currentBatchSize = 0;

            foreach (var messageBody in messageBodies)
            {
                var messageSize = Encoding.UTF8.GetByteCount(messageBody);

                if (currentBatch == null || currentBatch.Count == _maximumBatchSize || currentBatchSize + messageSize > MaximumMessageSize)
                {
                    currentBatch = [];
                    currentBatchSize = 0;

                    batches.Add(currentBatch);
                }

                currentBatch.Add(messageBody);
                currentBatchSize += messageSize;
            }

            return batches;
        }

        private string SerializeMessageBody(AMQPMessage body)
        {
            var messageBody = JsonConvert.SerializeObject(body);

            if (Encoding.UTF8.GetByteCount(messageBody) > MaximumMessageSize)
            {
                throw new Exception($"AMQP message for address {_address} is larger than {MaximumMessageSize} bytes");
            }

            return messageBody;
        }

        internal static Dictionary<string, MessageAttributeValue> GetMessageAttributes(double? timeOffsetSeconds)
        {
            var messageAttributes = new Dictionary<string, MessageAttributeValue>();

            if (timeOffsetSeconds > 0)
            {
                messageAttributes[NotBeforeAttributeName] = new MessageAttributeValue
                {
                    DataType = "Number",
                    StringValue = GetNotBeforeValue(timeOffsetSeconds.Value)
                };
            }

            return messageAttributes;
        }

        internal static string GetNotBeforeValue(double timeOffsetSeconds)
        {
            return DateTimeOffset.UtcNow.AddSeconds(timeOffsetSeconds).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
        }

        // SQS allows up to 128 printable ASCII characters; a longer session ID is hashed, which keeps it a distinct group

        internal static string GetMessageGroupId(string sessionId)
        {
            if (sessionId.Length <= _maximumMessageGroupIdLength && sessionId.All(character => character >= '!' && character <= '~'))
            {
                return sessionId;
            }

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sessionId)));
        }

        private static async Task<bool> TryDelayAsync(TimeSpan timeSpan, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(timeSpan, cancellationToken).ConfigureAwait(false);

                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        // The messages of one receive that are still waiting or being processed. A message leaves it before its visibility is
        // set for the last time (delete, hold, release), so the heartbeat never extends it afterwards.

        private sealed class Heartbeat : IDisposable
        {
            private readonly HashSet<ReceivedMessage> _pendingMessages;

            private readonly SemaphoreSlim _semaphore = new(1, 1);

            internal DateTimeOffset ReceivedAt { get; }

            internal Heartbeat(List<ReceivedMessage> receivedMessages)
            {
                _pendingMessages = [.. receivedMessages];

                ReceivedAt = receivedMessages[0].ReceivedAt;
            }

            internal async Task RemoveAsync(IEnumerable<ReceivedMessage> receivedMessages)
            {
                await _semaphore.WaitAsync().ConfigureAwait(false);

                try
                {
                    _pendingMessages.ExceptWith(receivedMessages);
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            internal async Task ExtendAsync(Func<List<ReceivedMessage>, Task> changeVisibilityAsync)
            {
                await _semaphore.WaitAsync().ConfigureAwait(false);

                try
                {
                    if (_pendingMessages.Count > 0)
                    {
                        await changeVisibilityAsync([.. _pendingMessages]).ConfigureAwait(false);
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            public void Dispose()
            {
                _semaphore.Dispose();
            }
        }

        private sealed record ReceivedMessage(Amazon.SQS.Model.Message Message, string Body, string GroupId, DateTimeOffset? NotBefore, DateTimeOffset ReceivedAt);
    }
}
