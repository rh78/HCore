using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCore.Amqp.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;

namespace HCore.Test.Amqp.Sqs
{
    // a queue without sessions: a FIFO queue on which every message has a group of its own, unless the sender passes one

    public class SqsPlainQueueTests
    {
        private const string _address = "Tasks";
        private const string _queueName = "Tasks.fifo";

        [Fact]
        public async Task Message_IsProcessedOnce_AndDeleted()
        {
            var aws = new FakeAws();
            var processor = new SqsTestProcessor("A");

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec(_address)]);

            await messenger.SendMessageAsync(_address, new SqsTestMessage("G", 1));

            await Wait.UntilAsync(() => processor.Successes().Count == 1 && aws.GetMessages(_queueName).Count == 0, TimeSpan.FromSeconds(5), "processed and deleted");

            var record = processor.Successes().Single();

            Assert.False(record.IsBatch);
            Assert.Equal(["G#1"], record.Keys);
        }

        [Fact]
        public async Task MessagesWithoutAGroup_GetAGroupOfTheirOwn()
        {
            var aws = new FakeAws();

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", new SqsTestProcessor("A"), [new QueueSpec(_address, Listeners: 0)]);

            await messenger.SendMessageAsync(_address, new SqsTestMessage("G", 1));
            await messenger.SendMessageAsync(_address, new SqsTestMessage("G", 2));

            Assert.Equal(2, aws.SentRequests.Select(request => request.MessageGroupId).Distinct().Count());
            Assert.All(aws.SentRequests, request => Assert.False(string.IsNullOrEmpty(request.MessageDeduplicationId)));
        }

        [Fact]
        public async Task SessionId_OnAQueueWithoutSessions_IsRejected()
        {
            var aws = new FakeAws();

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", new SqsTestProcessor("A"), [new QueueSpec(_address, Listeners: 0)]);

            var exception = await Assert.ThrowsAsync<Exception>(() => messenger.SendMessageAsync(_address, new SqsTestMessage("G", 1), sessionId: "S"));

            Assert.Contains("no session queue", exception.Message);
            Assert.Empty(aws.SentRequests);
        }

        [Fact]
        public async Task MessagesOfOneGroup_AreProcessedOneAfterAnother_InOrder()
        {
            var aws = new FakeAws();
            var processorA = new SqsTestProcessor("A") { Behaviour = _ => Task.Delay(50) };
            var processorB = new SqsTestProcessor("B", processorA.Records) { Behaviour = _ => Task.Delay(50) };

            await using var scope = new MessengerScope();

            var messengerA = await scope.StartAsync(aws, aws, "A", processorA, [new QueueSpec(_address, Listeners: 3)]);
            await scope.StartAsync(aws, aws, "B", processorB, [new QueueSpec(_address, Listeners: 3)]);

            for (var seq = 1; seq <= 10; seq++)
            {
                await messengerA.SendMessageAsync(_address, new SqsTestMessage("T", seq), messageGroupId: "tenant-1");
            }

            await Wait.UntilAsync(() => processorA.Successes().Count == 10, TimeSpan.FromSeconds(15), "group processed");

            OrderAssert.InOrderExactlyOnce(processorA.SuccessfulKeys(), new Dictionary<string, int> { ["T"] = 10 });

            Assert.All(aws.SentRequests, request => Assert.Equal("tenant-1", request.MessageGroupId));

            // one after another: no message of the group started before the previous one had finished

            var records = processorA.Records.OrderBy(record => record.At).ToList();

            for (var i = 1; i < records.Count; i++)
            {
                Assert.True(records[i].At - records[i - 1].At >= TimeSpan.FromMilliseconds(40), $"{records[i].Keys[0]} overlapped {records[i - 1].Keys[0]}");
            }
        }

        [Fact]
        public async Task ListenerReceivesOneMessageAtATime_SoALongMessageHoldsUpNoOtherMessage()
        {
            var aws = new FakeAws();
            var processor = new SqsTestProcessor("A")
            {
                Behaviour = call => call.Messages[0].Group == "Slow" ? Task.Delay(TimeSpan.FromSeconds(3)) : Task.CompletedTask
            };

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec(_address, Listeners: 2)]);

            await messenger.SendMessageAsync(_address, new SqsTestMessage("Slow", 1));
            await messenger.SendMessageAsync(_address, new SqsTestMessage("Fast", 1));

            await Wait.UntilAsync(() => processor.SuccessfulKeys().Contains("Fast#1"), TimeSpan.FromSeconds(2), "fast message processed while the slow one runs");

            Assert.DoesNotContain("Slow#1", processor.SuccessfulKeys());

            await Wait.UntilAsync(() => processor.Successes().Count == 2, TimeSpan.FromSeconds(6), "slow message processed");

            Assert.All(processor.Records, record => Assert.False(record.IsBatch));
        }

        [Fact]
        public async Task DelayedMessage_IsNotProcessedBeforeItIsDue()
        {
            var aws = new FakeAws();
            var processor = new SqsTestProcessor("A");

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec(_address)]);

            var sentAt = DateTimeOffset.UtcNow;

            await messenger.SendMessageAsync(_address, new SqsTestMessage("G", 1), timeOffsetSeconds: 2);

            await Wait.UntilAsync(() => processor.Successes().Count == 1, TimeSpan.FromSeconds(6), "delayed message processed");

            Assert.True(processor.Successes().Single().At - sentAt >= TimeSpan.FromSeconds(1.9));

            var request = aws.SentRequests.Single();

            // FIFO queues have no delay per message; the messenger holds the message until it is due

            Assert.Null(request.DelaySeconds);
            Assert.True(request.MessageAttributes.ContainsKey("HCoreNotBefore"));
        }

        [Fact]
        public async Task DelayedMessage_HoldsUpNoOtherMessage()
        {
            var aws = new FakeAws();
            var processor = new SqsTestProcessor("A");

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec(_address)]);

            await messenger.SendMessageAsync(_address, new SqsTestMessage("Delayed", 1), timeOffsetSeconds: 3);
            await messenger.SendMessageAsync(_address, new SqsTestMessage("Now", 1));

            await Wait.UntilAsync(() => processor.Successes().Count == 2, TimeSpan.FromSeconds(8), "both messages processed");

            Assert.Equal(["Now#1", "Delayed#1"], processor.SuccessfulKeys());
        }

        [Fact]
        public async Task DelayBeyondFifteenMinutes_IsHeldUntilItIsDue()
        {
            var aws = new FakeAws();
            var processor = new SqsTestProcessor("A");

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec(_address)]);

            await messenger.SendMessageAsync(_address, new SqsTestMessage("G", 1), timeOffsetSeconds: 3600);

            var request = aws.SentRequests.Single();

            Assert.Null(request.DelaySeconds);

            var notBefore = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(request.MessageAttributes["HCoreNotBefore"].StringValue));

            Assert.InRange(notBefore - DateTimeOffset.UtcNow, TimeSpan.FromMinutes(59), TimeSpan.FromMinutes(61));

            await Wait.UntilAsync(() => aws.GetMessages(_queueName).Single().IsInFlight, TimeSpan.FromSeconds(5), "message received and held");

            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.Empty(processor.Records);
        }

        [Fact]
        public async Task MessageArrivingBeforeItIsDue_IsHeldUntilItIsDue()
        {
            var aws = new FakeAws();
            var processor = new SqsTestProcessor("A");

            await using var scope = new MessengerScope();

            await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec(_address)]);

            // stored by another sender, due in 2 s

            var notBefore = DateTimeOffset.UtcNow.AddSeconds(2);

            await aws.SendMessageAsync(new Amazon.SQS.Model.SendMessageRequest(FakeAws.GetQueueUrl(_queueName), JsonConvert.SerializeObject(new SqsTestMessage("G", 1)))
            {
                MessageGroupId = "G",
                MessageDeduplicationId = Guid.NewGuid().ToString("N"),
                MessageAttributes = new()
                {
                    ["HCoreNotBefore"] = new Amazon.SQS.Model.MessageAttributeValue { DataType = "Number", StringValue = notBefore.ToUnixTimeMilliseconds().ToString() }
                }
            }, default);

            await Wait.UntilAsync(() => processor.Successes().Count == 1, TimeSpan.FromSeconds(6), "held message processed");

            Assert.True(processor.Successes().Single().At >= notBefore.AddMilliseconds(-100));
        }

        [Fact]
        public async Task RescheduleException_DeliversTheMessageAgainAtOnce()
        {
            var aws = new FakeAws();
            var processor = new SqsTestProcessor("A")
            {
                Behaviour = call => call.Attempt == 1 ? throw new RescheduleException() : Task.CompletedTask
            };

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec(_address)]);

            await messenger.SendMessageAsync(_address, new SqsTestMessage("G", 1));

            await Wait.UntilAsync(() => processor.Successes().Count == 1, TimeSpan.FromSeconds(5), "processed after reschedule");

            var records = processor.Records.ToList();

            Assert.Equal(["RescheduleException", "ok"], records.Select(record => record.Outcome));
            // the error hold would be 1 s

            Assert.True(records[1].At - records[0].At < TimeSpan.FromSeconds(0.5));
        }

        [Fact]
        public async Task PostponeException_HoldsTheMessageForItsTimeSpan()
        {
            var aws = new FakeAws();
            var processor = new SqsTestProcessor("A")
            {
                Behaviour = call => call.Attempt == 1 ? throw new PostponeException(TimeSpan.FromSeconds(2)) : Task.CompletedTask
            };

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec(_address)]);

            await messenger.SendMessageAsync(_address, new SqsTestMessage("G", 1));

            await Wait.UntilAsync(() => processor.Successes().Count == 1, TimeSpan.FromSeconds(6), "processed after postpone");

            var records = processor.Records.ToList();

            Assert.Equal(["PostponeException", "ok"], records.Select(record => record.Outcome));
            Assert.True(records[1].At - records[0].At >= TimeSpan.FromSeconds(1.9));
        }

        [Fact]
        public async Task Exception_IsLoggedAndTheMessageDeliveredAgainAfterTheErrorHold()
        {
            var aws = new FakeAws();
            var logger = new TestLogger();
            var processor = new SqsTestProcessor("A")
            {
                Behaviour = call => call.Attempt == 1 ? throw new InvalidOperationException("boom") : Task.CompletedTask
            };

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec(_address)], logger: logger);

            await messenger.SendMessageAsync(_address, new SqsTestMessage("G", 1));

            await Wait.UntilAsync(() => processor.Successes().Count == 1, TimeSpan.FromSeconds(6), "processed after error");

            var records = processor.Records.ToList();

            Assert.Equal(["InvalidOperationException", "ok"], records.Select(record => record.Outcome));
            Assert.True(records[1].At - records[0].At >= TimeSpan.FromSeconds(0.9));
            Assert.True(logger.Contains(LogLevel.Error, "boom"));
        }

        [Fact]
        public async Task LongProcessing_KeepsTheMessageInvisible_SoNoOtherMachineProcessesIt()
        {
            var aws = new FakeAws();
            var processorA = new SqsTestProcessor("A") { Behaviour = _ => Task.Delay(TimeSpan.FromSeconds(5)) };
            var processorB = new SqsTestProcessor("B", processorA.Records) { Behaviour = _ => Task.Delay(TimeSpan.FromSeconds(5)) };

            await using var scope = new MessengerScope();

            var messengerA = await scope.StartAsync(aws, aws, "A", processorA, [new QueueSpec(_address)]);
            await scope.StartAsync(aws, aws, "B", processorB, [new QueueSpec(_address)]);

            await messengerA.SendMessageAsync(_address, new SqsTestMessage("G", 1));

            await Wait.UntilAsync(() => processorA.Successes().Count == 1, TimeSpan.FromSeconds(10), "long message processed");

            // the visibility timeout is 2 s; without the heartbeat the other machine would have processed it as well

            await Task.Delay(TimeSpan.FromSeconds(3));

            Assert.Single(processorA.Records);
            Assert.Empty(aws.GetMessages(_queueName));
        }

        [Fact]
        public async Task ProcessingLongerThanTheMaximumVisibility_IsLogged()
        {
            var aws = new FakeAws();
            var logger = new TestLogger();
            var processor = new SqsTestProcessor("A") { Behaviour = call => call.Attempt == 1 ? Task.Delay(TimeSpan.FromSeconds(4)) : Task.CompletedTask };

            var timings = new HCore.Amqp.Sqs.SqsTimings
            {
                VisibilityTimeout = TimeSpan.FromSeconds(2),
                HeartbeatInterval = TimeSpan.FromMilliseconds(500),
                MaximumVisibility = TimeSpan.FromSeconds(3),
                ReceiveWaitTimeSeconds = 1,
                RetryDelay = TimeSpan.FromMilliseconds(100)
            };

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec(_address, Listeners: 2)], timings: timings, logger: logger);

            await messenger.SendMessageAsync(_address, new SqsTestMessage("G", 1));

            await Wait.UntilAsync(() => logger.Contains(LogLevel.Warning, "takes longer than"), TimeSpan.FromSeconds(6), "maximum visibility warning");
        }

        [Fact]
        public async Task ShutdownReceivers_FinishesTheMessageBeingProcessed_AndReceivesNothingMore()
        {
            var aws = new FakeAws();
            var processingStarted = new TaskCompletionSource();
            var processor = new SqsTestProcessor("A")
            {
                Behaviour = async _ =>
                {
                    processingStarted.TrySetResult();

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            };

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec(_address)]);

            await messenger.SendMessageAsync(_address, new SqsTestMessage("G", 1));

            await processingStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

            await messenger.ShutdownReceiversAsync();

            await messenger.SendMessageAsync(_address, new SqsTestMessage("G", 2));

            await messenger.WaitForTaskCompletionAsync().WaitAsync(TimeSpan.FromSeconds(10));

            await Wait.UntilAsync(() => processor.Successes().Count == 1, TimeSpan.FromSeconds(3), "message in progress completed");

            await Task.Delay(TimeSpan.FromSeconds(1.5));

            Assert.Equal(["G#1"], processor.SuccessfulKeys());
            Assert.Equal([2], aws.GetMessages(_queueName).Select(message => JsonConvert.DeserializeObject<SqsTestMessage>(message.Body).Seq));
        }

        [Fact]
        public async Task ManyMessages_OnTwoMachines_AreEachProcessedExactlyOnce()
        {
            var aws = new FakeAws();
            var processorA = new SqsTestProcessor("A") { Behaviour = _ => Task.Delay(50) };
            var processorB = new SqsTestProcessor("B", processorA.Records) { Behaviour = _ => Task.Delay(50) };

            await using var scope = new MessengerScope();

            var messengerA = await scope.StartAsync(aws, aws, "A", processorA, [new QueueSpec(_address, Listeners: 3)]);
            await scope.StartAsync(aws, aws, "B", processorB, [new QueueSpec(_address, Listeners: 3)]);

            for (var seq = 1; seq <= 60; seq++)
            {
                await messengerA.SendMessageAsync(_address, new SqsTestMessage("G", seq));
            }

            await Wait.UntilAsync(() => processorA.Successes().Count == 60, TimeSpan.FromSeconds(15), "all messages processed");

            var keys = processorA.SuccessfulKeys();

            Assert.Equal(60, keys.Distinct().Count());
            Assert.Contains(processorA.Records, record => record.Machine == "A");
            Assert.Contains(processorA.Records, record => record.Machine == "B");
        }
    }
}
