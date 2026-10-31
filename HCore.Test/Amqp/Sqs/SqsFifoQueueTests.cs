using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCore.Amqp.Exceptions;
using HCore.Amqp.Processor.Hosts;
using Xunit;

namespace HCore.Test.Amqp.Sqs
{
    public class SqsFifoQueueTests
    {
        private const string _address = "SessionTasks";
        private const string _queueName = "SessionTasks.fifo";

        [Fact]
        public async Task Groups_OnTwoMachines_AreProcessedInOrder_AndExactlyOnce()
        {
            var aws = new FakeAws();
            var processorA = new SqsTestProcessor("A") { Behaviour = _ => Task.Delay(Random.Shared.Next(5, 40)) };
            var processorB = new SqsTestProcessor("B", processorA.Records) { Behaviour = _ => Task.Delay(Random.Shared.Next(5, 40)) };

            await using var scope = new MessengerScope();

            var messengerA = await scope.StartAsync(aws, aws, "A", processorA, [new QueueSpec(_address, Listeners: 2, IsSession: true)]);
            await scope.StartAsync(aws, aws, "B", processorB, [new QueueSpec(_address, Listeners: 2, IsSession: true)]);

            var groups = new[] { "G1", "G2", "G3", "G4", "G5", "G6" };

            for (var seq = 1; seq <= 10; seq++)
            {
                foreach (var group in groups)
                {
                    await messengerA.SendMessageAsync(_address, new SqsTestMessage(group, seq), sessionId: group);
                }
            }

            await Wait.UntilAsync(() => processorA.SuccessfulKeys().Count == 60, TimeSpan.FromSeconds(20), "all groups processed");

            OrderAssert.InOrderExactlyOnce(processorA.SuccessfulKeys(), groups.ToDictionary(group => group, _ => 10));

            Assert.All(processorA.Records, record => Assert.True(record.IsBatch));
            Assert.Contains(processorA.Records, record => record.Machine == "A");
            Assert.Contains(processorA.Records, record => record.Machine == "B");
            Assert.Empty(aws.GetMessages(_queueName));
        }

        [Fact]
        public async Task PostponingGroup_BlocksOnlyItself_AndKeepsItsMessagesInOrder()
        {
            var aws = new FakeAws();
            var processor = new SqsTestProcessor("A")
            {
                Behaviour = call => call.Messages[0].Group == "P" ? throw new PostponeException(TimeSpan.FromSeconds(1)) : Task.CompletedTask
            };

            await using var scope = new MessengerScope();

            // a single listener: on ActiveMQ a postponing group bound to it blocked every other group

            var messenger = await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec(_address, Listeners: 1, IsSession: true)]);

            for (var seq = 1; seq <= 5; seq++)
            {
                await messenger.SendMessageAsync(_address, new SqsTestMessage("P", seq), sessionId: "P");

                foreach (var group in new[] { "G1", "G2", "G3" })
                {
                    await messenger.SendMessageAsync(_address, new SqsTestMessage(group, seq), sessionId: group);
                }
            }

            await Wait.UntilAsync(() => processor.SuccessfulKeys().Count == 15, TimeSpan.FromSeconds(15), "healthy groups processed");

            OrderAssert.InOrderExactlyOnce(processor.SuccessfulKeys(), new Dictionary<string, int> { ["G1"] = 5, ["G2"] = 5, ["G3"] = 5 });

            Assert.DoesNotContain(processor.SuccessfulKeys(), key => key.StartsWith("P#"));

            var remaining = aws.GetMessages(_queueName);

            Assert.Equal(["P", "P", "P", "P", "P"], remaining.Select(message => message.GroupId));
            Assert.Equal([1, 2, 3, 4, 5], remaining.Select(message => Newtonsoft.Json.JsonConvert.DeserializeObject<SqsTestMessage>(message.Body).Seq));
        }

        [Fact]
        public async Task FailingGroup_IsRetriedInOrder_OnceTheErrorIsGone()
        {
            var aws = new FakeAws();
            var processor = new SqsTestProcessor("A")
            {
                Behaviour = call => call.Messages[0].ToString() == "E#1" && call.Attempt <= 2 ? throw new InvalidOperationException("boom") : Task.CompletedTask
            };

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec(_address, Listeners: 2, IsSession: true)]);

            for (var seq = 1; seq <= 3; seq++)
            {
                await messenger.SendMessageAsync(_address, new SqsTestMessage("E", seq), sessionId: "E");
            }

            await Wait.UntilAsync(() => processor.SuccessfulKeys().Count == 3, TimeSpan.FromSeconds(10), "failing group processed");

            OrderAssert.InOrderExactlyOnce(processor.SuccessfulKeys(), new Dictionary<string, int> { ["E"] = 3 });
        }

        [Fact]
        public async Task WaitingMessagesOfAGroup_AreProcessedTogether()
        {
            var aws = new FakeAws();
            var sender = new SqsTestProcessor("S");
            var processor = new SqsTestProcessor("A");

            await using var scope = new MessengerScope();

            var senderMessenger = await scope.StartAsync(aws, aws, "S", sender, [new QueueSpec(_address, Listeners: 0, IsSession: true)]);

            for (var seq = 1; seq <= 4; seq++)
            {
                await senderMessenger.SendMessageAsync(_address, new SqsTestMessage("G", seq), sessionId: "G");
            }

            await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec(_address, Listeners: 1, IsSession: true)]);

            await Wait.UntilAsync(() => processor.SuccessfulKeys().Count == 4, TimeSpan.FromSeconds(5), "group processed");

            // like the Service Bus session batch, which lets the processors merge duplicate tasks

            var record = Assert.Single(processor.Successes());

            Assert.Equal(["G#1", "G#2", "G#3", "G#4"], record.Keys);
        }

        [Fact]
        public async Task MixedBatch_IsProcessedOneGroupAtATime_AndTheOtherGroupsAreReleased()
        {
            var aws = new FakeAws();
            var sender = new SqsTestProcessor("S");
            var processor = new SqsTestProcessor("A") { Behaviour = _ => Task.Delay(200) };

            await using var scope = new MessengerScope();

            var senderMessenger = await scope.StartAsync(aws, aws, "S", sender, [new QueueSpec(_address, Listeners: 0, IsSession: true)]);

            // interleaved, so one receive returns messages of several groups

            for (var seq = 1; seq <= 2; seq++)
            {
                foreach (var group in new[] { "A", "B", "C" })
                {
                    await senderMessenger.SendMessageAsync(_address, new SqsTestMessage(group, seq), sessionId: group);
                }
            }

            await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec(_address, Listeners: 1, IsSession: true)]);

            await Wait.UntilAsync(() => processor.SuccessfulKeys().Count == 6, TimeSpan.FromSeconds(10), "all groups processed");

            var records = processor.Successes();

            Assert.All(records, record => Assert.Single(record.Keys.Select(key => key.Split('#')[0]).Distinct()));

            OrderAssert.InOrderExactlyOnce(processor.SuccessfulKeys(), new Dictionary<string, int> { ["A"] = 2, ["B"] = 2, ["C"] = 2 });
        }

        [Fact]
        public async Task DelayedMessage_IsProcessedWhenDue_AndTheGroupStaysInOrder()
        {
            var aws = new FakeAws();
            var processor = new SqsTestProcessor("A");

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec(_address, Listeners: 2, IsSession: true)]);

            var sentAt = DateTimeOffset.UtcNow;

            await messenger.SendMessageAsync(_address, new SqsTestMessage("G", 1), timeOffsetSeconds: 2, sessionId: "G");
            await messenger.SendMessageAsync(_address, new SqsTestMessage("G", 2), sessionId: "G");
            await messenger.SendMessageAsync(_address, new SqsTestMessage("H", 1), sessionId: "H");

            await Wait.UntilAsync(() => processor.SuccessfulKeys().Count == 3, TimeSpan.FromSeconds(8), "all messages processed");

            var successes = processor.Successes();

            var delayedRecord = successes.Single(record => record.Keys.Contains("G#1"));

            Assert.True(delayedRecord.At - sentAt >= TimeSpan.FromSeconds(1.9));
            Assert.True(successes.Single(record => record.Keys.Contains("H#1")).At < delayedRecord.At);

            OrderAssert.InOrderExactlyOnce(processor.SuccessfulKeys(), new Dictionary<string, int> { ["G"] = 2, ["H"] = 1 });

            // FIFO queues reject a delay per message

            Assert.All(aws.SentRequests, request => Assert.Equal(0, request.DelaySeconds));
        }

        [Fact]
        public async Task SessionId_IsTheMessageGroup_WithAGuidDeduplicationId()
        {
            var aws = new FakeAws();

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", new SqsTestProcessor("A"), [new QueueSpec(_address, Listeners: 0, IsSession: true)]);

            await messenger.SendMessageAsync(_address, new SqsTestMessage("G", 1), sessionId: "connector-1");
            await messenger.SendMessageAsync(_address, new SqsTestMessage("G", 1), sessionId: "connector-1");

            Assert.All(aws.SentRequests, request => Assert.Equal("connector-1", request.MessageGroupId));
            Assert.All(aws.SentRequests, request => Assert.True(Guid.TryParse(request.MessageDeduplicationId, out _)));

            // identical bodies are both kept: the sync chain sends the same task again on purpose

            Assert.Equal(2, aws.GetMessages(_queueName).Count);
        }

        [Fact]
        public async Task SessionIdLongerThanAllowed_IsHashed_AndStaysOneGroup()
        {
            var aws = new FakeAws();
            var processor = new SqsTestProcessor("A");

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec(_address, Listeners: 2, IsSession: true)]);

            var sessionId = $"{Guid.NewGuid()}_{Guid.NewGuid()}_{Guid.NewGuid()}_PerformanceAnalysisContext";

            Assert.True(sessionId.Length > 128);

            for (var seq = 1; seq <= 5; seq++)
            {
                await messenger.SendMessageAsync(_address, new SqsTestMessage("L", seq), sessionId: sessionId);
            }

            await Wait.UntilAsync(() => processor.SuccessfulKeys().Count == 5, TimeSpan.FromSeconds(5), "long session processed");

            OrderAssert.InOrderExactlyOnce(processor.SuccessfulKeys(), new Dictionary<string, int> { ["L"] = 5 });

            Assert.Single(aws.SentRequests.Select(request => request.MessageGroupId).Distinct());
            Assert.Equal(SqsQueueHost.GetMessageGroupId(sessionId), aws.SentRequests[0].MessageGroupId);
            Assert.True(aws.SentRequests[0].MessageGroupId.Length <= 128);
        }

        [Fact]
        public async Task SessionQueue_RequiresASessionId_AndOtherQueuesRejectOne()
        {
            var aws = new FakeAws();

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", new SqsTestProcessor("A"), [new QueueSpec(_address, Listeners: 0, IsSession: true), new QueueSpec("Plain", Listeners: 0)]);

            var missing = await Assert.ThrowsAsync<Exception>(() => messenger.SendMessageAsync(_address, new SqsTestMessage("G", 1)));
            var unexpected = await Assert.ThrowsAsync<Exception>(() => messenger.SendMessageAsync("Plain", new SqsTestMessage("G", 1), sessionId: "G"));

            Assert.Contains("Session ID is missing", missing.Message);
            Assert.Contains("no session queue", unexpected.Message);
            Assert.Empty(aws.SentRequests);
        }

        [Fact]
        public async Task ManyGroupsOnOneListener_WithFastProcessing_AreNotStalledByThrottledHandBacks()
        {
            var aws = new FakeAws();
            var logger = new TestLogger();
            var processor = new SqsTestProcessor("A") { Behaviour = _ => Task.Delay(20) };

            await using var scope = new MessengerScope();

            // the IntegrationLayer task queue: one listener, many sessions waiting, fast tasks. The listener receives the groups
            // it handed back a moment ago and hands them back again within the throttle window

            var messenger = await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec(_address, Listeners: 1, IsSession: true)], logger: logger);

            var groups = Enumerable.Range(1, 20).Select(index => $"G{index}").ToArray();

            for (var seq = 1; seq <= 3; seq++)
            {
                foreach (var group in groups)
                {
                    await messenger.SendMessageAsync(_address, new SqsTestMessage(group, seq), sessionId: group);
                }
            }

            await Wait.UntilAsync(() => processor.SuccessfulKeys().Count == 60, TimeSpan.FromSeconds(30), "all groups processed");

            OrderAssert.InOrderExactlyOnce(processor.SuccessfulKeys(), groups.ToDictionary(group => group, _ => 3));

            Assert.True(aws.ThrottledVisibilityChanges > 0, "the scenario throttles hand-backs");
            Assert.False(logger.Contains(Microsoft.Extensions.Logging.LogLevel.Warning, "could not be changed"), "every throttled hand-back was sent again");
        }

        [Fact]
        public async Task VisibilityChange_ThrottledOnEveryAttempt_IsGivenUpAfterFiveAttempts_AndTheMessageStillFollows()
        {
            var aws = new FakeAws { FifoVisibilityThrottleWindow = TimeSpan.FromHours(1) };
            var logger = new TestLogger();

            // the heartbeat changes the visibility first, so the postpone hold that follows is throttled every time

            var processor = new SqsTestProcessor("A")
            {
                Behaviour = async call =>
                {
                    if (call.Attempt == 1)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(0.8));

                        throw new PostponeException(TimeSpan.FromMilliseconds(100));
                    }
                }
            };

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec(_address, Listeners: 1, IsSession: true)], logger: logger);

            await messenger.SendMessageAsync(_address, new SqsTestMessage("G", 1), sessionId: "G");

            await Wait.UntilAsync(() => logger.Contains(Microsoft.Extensions.Logging.LogLevel.Warning, "RequestThrottled"), TimeSpan.FromSeconds(10), "throttle warning after the last attempt");

            Assert.Equal(5, aws.ThrottledVisibilityChanges);

            // the hold could not be set, so the message follows once the heartbeat's visibility ends

            await Wait.UntilAsync(() => processor.Successes().Count == 1, TimeSpan.FromSeconds(10), "processed after the visibility ended");
        }
    }
}
