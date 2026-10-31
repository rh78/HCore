using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace HCore.Test.Amqp.Sqs
{
    public class SqsTopicTests
    {
        private const string _queueAddress = "Tasks";
        private const string _topicAddress = "CacheTasks";

        [Fact]
        public async Task TopicMessage_IsProcessedOnceByEveryMachine()
        {
            var aws = new FakeAws();
            var processorA = new SqsTestProcessor("A");
            var processorB = new SqsTestProcessor("B", processorA.Records);

            await using var scope = new MessengerScope();

            var messengerA = await scope.StartAsync(aws, aws, "A", processorA, [new QueueSpec(_queueAddress)], [new TopicSpec(_topicAddress, Listeners: 2)]);
            await scope.StartAsync(aws, aws, "B", processorB, [new QueueSpec(_queueAddress)], [new TopicSpec(_topicAddress, Listeners: 2)]);

            await messengerA.SendMessageAsync(_topicAddress, new SqsTestMessage("T", 1));
            await messengerA.SendMessageAsync(_topicAddress, new SqsTestMessage("T", 2));

            await Wait.UntilAsync(() => processorA.Successes(_topicAddress).Count == 4, TimeSpan.FromSeconds(5), "both machines processed both messages");

            await Task.Delay(TimeSpan.FromSeconds(1));

            var records = processorA.Successes(_topicAddress);

            Assert.Equal(4, records.Count);
            Assert.Equal(["T#1", "T#2"], records.Where(record => record.Machine == "A").SelectMany(record => record.Keys).Order());
            Assert.Equal(["T#1", "T#2"], records.Where(record => record.Machine == "B").SelectMany(record => record.Keys).Order());
            Assert.All(records, record => Assert.False(record.IsBatch));
        }

        [Fact]
        public async Task CreateResources_CreatesTheTopic_AndAQueuePerMachine_SubscribedWithRawDelivery()
        {
            var aws = new FakeAws();

            await using var scope = new MessengerScope();

            await scope.StartAsync(aws, aws, "web-01.internal", new SqsTestProcessor("A"), [new QueueSpec(_queueAddress)], [new TopicSpec(_topicAddress)]);

            Assert.Equal([_topicAddress], aws.CreatedTopicNames);

            var queueName = $"{_topicAddress}-web-01-internal";

            Assert.True(aws.HasQueue(queueName));

            var attributes = aws.GetAttributes(queueName);

            Assert.Equal("60", attributes["MessageRetentionPeriod"]);
            Assert.Contains(FakeAws.GetTopicArn(_topicAddress), attributes["Policy"]);
            Assert.Contains("sns.amazonaws.com", attributes["Policy"]);
            Assert.Contains(FakeAws.GetQueueArn(queueName), attributes["Policy"]);

            var subscription = Assert.Single(aws.GetSubscriptions(_topicAddress));

            Assert.Equal(FakeAws.GetQueueArn(queueName), subscription.QueueArn);
            Assert.True(subscription.RawMessageDelivery);
        }

        [Fact]
        public async Task TopicWithoutListeners_CreatesNoQueue_ButCanBeSentTo()
        {
            var aws = new FakeAws();
            var processor = new SqsTestProcessor("B");

            await using var scope = new MessengerScope();

            var sender = await scope.StartAsync(aws, aws, "A", new SqsTestProcessor("A"), [new QueueSpec(_queueAddress)], [new TopicSpec(_topicAddress, Listeners: 0)]);
            await scope.StartAsync(aws, aws, "B", processor, [new QueueSpec(_queueAddress)], [new TopicSpec(_topicAddress)]);

            Assert.False(aws.HasQueue($"{_topicAddress}-A"));

            await sender.SendMessageAsync(_topicAddress, new SqsTestMessage("T", 1));

            await Wait.UntilAsync(() => processor.Successes(_topicAddress).Count == 1, TimeSpan.FromSeconds(5), "topic message processed");
        }

        [Fact]
        public async Task SubscriptionWithoutRawDelivery_IsUnwrapped()
        {
            var aws = new FakeAws();
            var processor = new SqsTestProcessor("A");

            var topicQueueName = $"{_topicAddress}-A";

            aws.AddQueue(_queueAddress);
            aws.AddQueue(topicQueueName);
            aws.AddTopic(_topicAddress);
            aws.AllowTopic(topicQueueName, _topicAddress);
            aws.Subscribe(_topicAddress, topicQueueName, rawMessageDelivery: false);

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec(_queueAddress)], [new TopicSpec(_topicAddress)], connectionString: MessengerScope.TestCredentials);

            var sentAt = DateTimeOffset.UtcNow;

            await messenger.SendMessageAsync(_topicAddress, new SqsTestMessage("T", 1), timeOffsetSeconds: 2);

            await Wait.UntilAsync(() => processor.Successes(_topicAddress).Count == 1, TimeSpan.FromSeconds(6), "enveloped message processed");

            var record = processor.Successes(_topicAddress).Single();

            Assert.Equal(["T#1"], record.Keys);
            Assert.True(record.At - sentAt >= TimeSpan.FromSeconds(1.9));
        }

        [Fact]
        public async Task DelayedTopicMessage_IsHeldOnEveryMachineUntilDue()
        {
            var aws = new FakeAws();
            var processorA = new SqsTestProcessor("A");
            var processorB = new SqsTestProcessor("B", processorA.Records);

            await using var scope = new MessengerScope();

            var messengerA = await scope.StartAsync(aws, aws, "A", processorA, [new QueueSpec(_queueAddress)], [new TopicSpec(_topicAddress)]);
            await scope.StartAsync(aws, aws, "B", processorB, [new QueueSpec(_queueAddress)], [new TopicSpec(_topicAddress)]);

            var sentAt = DateTimeOffset.UtcNow;

            await messengerA.SendMessageAsync(_topicAddress, new SqsTestMessage("T", 1), timeOffsetSeconds: 2);

            await Wait.UntilAsync(() => processorA.Successes(_topicAddress).Count == 2, TimeSpan.FromSeconds(6), "both machines processed");

            Assert.All(processorA.Successes(_topicAddress), record => Assert.True(record.At - sentAt >= TimeSpan.FromSeconds(1.9)));
        }

        [Fact]
        public async Task TopicMessageDelayedBeyondTheRetention_IsRejected()
        {
            var aws = new FakeAws();

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", new SqsTestProcessor("A"), [new QueueSpec(_queueAddress)], [new TopicSpec(_topicAddress)]);

            var exception = await Assert.ThrowsAsync<Exception>(() => messenger.SendMessageAsync(_topicAddress, new SqsTestMessage("T", 1), timeOffsetSeconds: 60));

            Assert.Contains("can not be delayed", exception.Message);
        }

        [Fact]
        public async Task BatchSendToATopic_IsNotSupported()
        {
            var aws = new FakeAws();

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", new SqsTestProcessor("A"), [new QueueSpec(_queueAddress)], [new TopicSpec(_topicAddress)]);

            await Assert.ThrowsAsync<NotImplementedException>(() => messenger.SendMessagesAsync(_topicAddress, new[] { new SqsTestMessage("T", 1) }));
        }
    }
}
