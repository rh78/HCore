using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCore.Amqp.Messenger.Impl;
using HCore.Amqp.Sqs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;

namespace HCore.Test.Amqp.Sqs
{
    public class SqsSetupAndSendTests
    {
        [Fact]
        public async Task CreateResources_CreatesFifoAndStandardQueues_WithTheAgreedAttributes()
        {
            var aws = new FakeAws();

            await using var scope = new MessengerScope();

            await scope.StartAsync(aws, aws, "A", new SqsTestProcessor("A"), [new QueueSpec("Sessions", IsSession: true), new QueueSpec("Plain")]);

            Assert.Equal(["Sessions.fifo", "Plain"], aws.CreatedQueueNames);

            var fifoAttributes = aws.GetAttributes("Sessions.fifo");

            Assert.Equal("true", fifoAttributes["FifoQueue"]);
            Assert.Equal("false", fifoAttributes["ContentBasedDeduplication"]);
            Assert.Equal("messageGroup", fifoAttributes["DeduplicationScope"]);
            Assert.Equal("perMessageGroupId", fifoAttributes["FifoThroughputLimit"]);
            Assert.Equal("true", fifoAttributes["SqsManagedSseEnabled"]);
            Assert.Equal("1209600", fifoAttributes["MessageRetentionPeriod"]);
            Assert.False(fifoAttributes.ContainsKey("RedrivePolicy"));
            Assert.False(fifoAttributes.ContainsKey("DelaySeconds"));

            var standardAttributes = aws.GetAttributes("Plain");

            Assert.False(standardAttributes.ContainsKey("FifoQueue"));
            Assert.Equal("true", standardAttributes["SqsManagedSseEnabled"]);
        }

        [Fact]
        public async Task MaxReceiveCount_CreatesADeadLetterQueue_ThatReceivesAMessageFailingTooOften()
        {
            var aws = new FakeAws();
            var processor = new SqsTestProcessor("A") { Behaviour = _ => throw new InvalidOperationException("always") };

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec("Sessions", IsSession: true, MaxReceiveCount: 2), new QueueSpec("Plain", MaxReceiveCount: 2)]);

            Assert.True(aws.HasQueue("Sessions-dlq.fifo"));
            Assert.True(aws.HasQueue("Plain-dlq"));
            Assert.Equal("true", aws.GetAttributes("Sessions-dlq.fifo")["FifoQueue"]);

            var redrivePolicy = JsonConvert.DeserializeAnonymousType(aws.GetAttributes("Plain")["RedrivePolicy"], new { deadLetterTargetArn = "", maxReceiveCount = 0 });

            Assert.Equal(FakeAws.GetQueueArn("Plain-dlq"), redrivePolicy.deadLetterTargetArn);
            Assert.Equal(2, redrivePolicy.maxReceiveCount);

            await messenger.SendMessageAsync("Plain", new SqsTestMessage("G", 1));

            await Wait.UntilAsync(() => aws.GetMessages("Plain-dlq").Count == 1, TimeSpan.FromSeconds(8), "message moved to the dead-letter queue");

            Assert.Equal(2, processor.Records.Count);
            Assert.Empty(aws.GetMessages("Plain"));
        }

        [Fact]
        public async Task WithoutCreateResources_AMissingQueueFailsTheStart()
        {
            var aws = new FakeAws();

            await using var scope = new MessengerScope();

            var exception = await Assert.ThrowsAsync<Exception>(() => scope.StartAsync(aws, aws, "A", new SqsTestProcessor("A"), [new QueueSpec("Sessions", IsSession: true)], connectionString: MessengerScope.TestCredentials));

            Assert.Contains("Sessions.fifo", exception.Message);
            Assert.Empty(aws.CreatedQueueNames);
        }

        [Fact]
        public async Task WithoutCreateResources_ExistingResourcesAreUsed_AndNothingIsCreated()
        {
            var aws = new FakeAws();
            var processor = new SqsTestProcessor("A");

            // as Terraform creates them

            aws.AddQueue("Prod-Sessions.fifo");
            aws.AddQueue("Prod-Plain");
            aws.AddQueue("Prod-CacheTasks-web-01");
            aws.AddTopic("Prod-CacheTasks");
            aws.AllowTopic("Prod-CacheTasks-web-01", "Prod-CacheTasks");
            aws.Subscribe("Prod-CacheTasks", "Prod-CacheTasks-web-01", rawMessageDelivery: true);

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "web-01", processor, [new QueueSpec("Sessions", IsSession: true), new QueueSpec("Plain")], [new TopicSpec("CacheTasks")],
                connectionString: $"{MessengerScope.TestCredentials};QueueNamePrefix=Prod-");

            await messenger.SendMessageAsync("Sessions", new SqsTestMessage("S", 1), sessionId: "S");
            await messenger.SendMessageAsync("Plain", new SqsTestMessage("P", 1));
            await messenger.SendMessageAsync("CacheTasks", new SqsTestMessage("T", 1));

            await Wait.UntilAsync(() => processor.SuccessfulKeys().Count == 3, TimeSpan.FromSeconds(5), "all addresses processed");

            Assert.Empty(aws.CreatedQueueNames);
            Assert.Empty(aws.CreatedTopicNames);
            Assert.Single(aws.GetSubscriptions("Prod-CacheTasks"));
            Assert.Equal(["P#1", "S#1", "T#1"], processor.SuccessfulKeys().Order());
        }

        [Fact]
        public async Task ExistingQueueWithOtherAttributes_IsUsedAsItIs()
        {
            var aws = new FakeAws();

            aws.AddQueue("Plain", new Dictionary<string, string> { ["VisibilityTimeout"] = "300" });

            await using var scope = new MessengerScope();

            await scope.StartAsync(aws, aws, "A", new SqsTestProcessor("A"), [new QueueSpec("Plain")]);

            Assert.Empty(aws.CreatedQueueNames);
            Assert.Equal("300", aws.GetAttributes("Plain")["VisibilityTimeout"]);
        }

        [Fact]
        public async Task SendMessages_SendsBatchesOfTen_AsSeparateMessages()
        {
            var aws = new FakeAws();
            var processor = new SqsTestProcessor("A");

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec("Plain", Listeners: 3)]);

            await messenger.SendMessagesAsync("Plain", Enumerable.Range(1, 25).Select(seq => new SqsTestMessage("B", seq)).ToList());

            Assert.Equal([10, 10, 5], aws.SentBatchRequests.Select(request => request.Entries.Count));

            await Wait.UntilAsync(() => processor.SuccessfulKeys().Count == 25, TimeSpan.FromSeconds(10), "batch processed");

            Assert.Equal(25, processor.SuccessfulKeys().Distinct().Count());
            Assert.All(processor.Records, record => Assert.Single(record.Keys));
        }

        [Fact]
        public async Task SendMessages_SplitsBatchesByTheirSize()
        {
            var aws = new FakeAws();

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", new SqsTestProcessor("A"), [new QueueSpec("Plain", Listeners: 0)]);

            var payload = new string('x', 100 * 1024);

            await messenger.SendMessagesAsync("Plain", Enumerable.Range(1, 5).Select(seq => new SqsTestMessage("B", seq, payload)).ToList());

            Assert.Equal([2, 2, 1], aws.SentBatchRequests.Select(request => request.Entries.Count));
            Assert.Equal(5, aws.GetMessages("Plain").Count);
        }

        [Fact]
        public async Task SendMessages_ResendsOnlyTheFailedEntries()
        {
            var aws = new FakeAws();
            var logger = new TestLogger();

            var failedOnce = new HashSet<string>();

            aws.FailBatchEntry = entry =>
            {
                var message = JsonConvert.DeserializeObject<SqsTestMessage>(entry.MessageBody);

                return message.Seq % 3 == 0 && failedOnce.Add(message.ToString());
            };

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", new SqsTestProcessor("A"), [new QueueSpec("Plain", Listeners: 0)], logger: logger);

            await messenger.SendMessagesAsync("Plain", Enumerable.Range(1, 10).Select(seq => new SqsTestMessage("B", seq)).ToList());

            var seqs = aws.GetMessages("Plain").Select(message => JsonConvert.DeserializeObject<SqsTestMessage>(message.Body).Seq).Order().ToList();

            Assert.Equal(Enumerable.Range(1, 10).ToList(), seqs);
            Assert.Equal([10, 3], aws.SentBatchRequests.Select(request => request.Entries.Count));
            Assert.True(logger.Contains(LogLevel.Error, "SQS batch send failed for 3 message(s)"));
        }

        [Fact]
        public async Task SendMessages_WithASessionOrADelay_IsNotSupported()
        {
            var aws = new FakeAws();

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", new SqsTestProcessor("A"), [new QueueSpec("Plain", Listeners: 0), new QueueSpec("Sessions", Listeners: 0, IsSession: true)]);

            var messages = new[] { new SqsTestMessage("B", 1) };

            await Assert.ThrowsAsync<NotImplementedException>(() => messenger.SendMessagesAsync("Plain", messages, timeOffsetSeconds: 5));
            await Assert.ThrowsAsync<NotImplementedException>(() => messenger.SendMessagesAsync("Sessions", messages, sessionId: "S"));
        }

        [Fact]
        public async Task FailingSend_IsRetried_UntilTheMessageIsStored()
        {
            var aws = new FakeAws();
            var logger = new TestLogger();

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", new SqsTestProcessor("A"), [new QueueSpec("Plain", Listeners: 0)], logger: logger);

            aws.FailingSendCount = 2;

            await messenger.SendMessageAsync("Plain", new SqsTestMessage("G", 1));

            Assert.Single(aws.GetMessages("Plain"));
            Assert.Equal(2, logger.Entries.Count(entry => entry.Level == LogLevel.Error && entry.Message.Contains("AMQP exception in sender for address Plain")));
        }

        [Fact]
        public async Task TooLargeMessage_FailsAtOnce()
        {
            var aws = new FakeAws();

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", new SqsTestProcessor("A"), [new QueueSpec("Plain", Listeners: 0)], [new TopicSpec("Topic", Listeners: 0)]);

            var payload = new string('x', 257 * 1024);

            var queueException = await Assert.ThrowsAsync<Exception>(() => messenger.SendMessageAsync("Plain", new SqsTestMessage("G", 1, payload)));
            var topicException = await Assert.ThrowsAsync<Exception>(() => messenger.SendMessageAsync("Topic", new SqsTestMessage("G", 1, payload)));

            Assert.Contains("larger than", queueException.Message);
            Assert.Contains("larger than", topicException.Message);
        }

        [Fact]
        public async Task UnknownAddress_FailsAtOnce()
        {
            var aws = new FakeAws();

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", new SqsTestProcessor("A"), [new QueueSpec("Plain", Listeners: 0)]);

            var exception = await Assert.ThrowsAsync<Exception>(() => messenger.SendMessageAsync("Other", new SqsTestMessage("G", 1)));

            Assert.Contains("not available", exception.Message);
        }

        [Fact]
        public async Task SendAfterShutdown_Fails()
        {
            var aws = new FakeAws();

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", new SqsTestProcessor("A"), [new QueueSpec("Plain", Listeners: 0)]);

            await messenger.ShutdownAsync();

            var exception = await Assert.ThrowsAsync<Exception>(() => messenger.SendMessageAsync("Plain", new SqsTestMessage("G", 1)));

            Assert.Contains("cancellation is requested", exception.Message);
        }

        [Fact]
        public async Task SendMessageTrySynchronousFirst_ProcessesDirectly_AndQueuesOnFailure()
        {
            var aws = new FakeAws();
            var processor = new SqsTestProcessor("A")
            {
                Behaviour = call => call.Messages[0].Seq == 2 && call.Attempt == 1 ? throw new InvalidOperationException("direct failure") : Task.CompletedTask
            };

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec("Plain")]);

            await messenger.SendMessageTrySynchronousFirstAsync("Plain", new SqsTestMessage("G", 1));

            Assert.Equal(["G#1"], processor.SuccessfulKeys());
            Assert.Empty(aws.SentRequests);

            await messenger.SendMessageTrySynchronousFirstAsync("Plain", new SqsTestMessage("G", 2));

            Assert.Single(aws.SentRequests);

            await Wait.UntilAsync(() => processor.SuccessfulKeys().Count == 2, TimeSpan.FromSeconds(5), "queued message processed");
        }

        [Fact]
        public async Task IsAvailable_TellsWhetherTheQueueExists()
        {
            var aws = new FakeAws();

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", new SqsTestProcessor("A"), [new QueueSpec("Plain", Listeners: 0)]);

            Assert.True(await messenger.IsAvailableAsync());

            aws.RemoveQueue("Plain");

            Assert.False(await messenger.IsAvailableAsync());
        }

        [Fact]
        public void TopicSessions_AreRejected()
        {
            var aws = new FakeAws();

            var exception = Assert.Throws<Exception>(() => new SqsMessengerImpl(SqsConnectionSettings.Parse(MessengerScope.TestCredentials), aws, aws, new SqsTimings(), "A",
                ["Plain"], ["Topic"], [1], [1], [false], [true], [null], new SqsTestProcessor("A"), new TestLogger()));

            Assert.Contains("topic sessions", exception.Message);
        }
    }
}
