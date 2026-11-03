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
        public async Task MixedReceive_IsProcessedGroupByGroup_EachGroupAsOneBatch()
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

            // one receive returned all six; every group was processed as one batch of its two messages, nothing went back

            Assert.Equal(3, records.Count);
            Assert.All(records, record => Assert.Single(record.Keys.Select(key => key.Split('#')[0]).Distinct()));
            Assert.All(records, record => Assert.Equal(2, record.Keys.Count));
            Assert.Equal(0, aws.ThrottledVisibilityChanges);

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
        public async Task ManyGroupsOnOneListener_WithFastProcessing_AreProcessedWithoutStalls()
        {
            var aws = new FakeAws();
            var logger = new TestLogger();
            var processor = new SqsTestProcessor("A") { Behaviour = _ => Task.Delay(20) };

            await using var scope = new MessengerScope();

            // the IntegrationLayer task queue: one listener, many sessions waiting, fast tasks. Handing the other groups back
            // made SQS throttle the second hand-back of the same messages, which then stalled for the visibility timeout

            var messenger = await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec(_address, Listeners: 1, IsSession: true)], logger: logger);

            var groups = Enumerable.Range(1, 20).Select(index => $"G{index}").ToArray();

            for (var seq = 1; seq <= 3; seq++)
            {
                foreach (var group in groups)
                {
                    await messenger.SendMessageAsync(_address, new SqsTestMessage(group, seq), sessionId: group);
                }
            }

            // with a stall every hand-back would wait for the visibility timeout of 2 s

            await Wait.UntilAsync(() => processor.SuccessfulKeys().Count == 60, TimeSpan.FromSeconds(6), "all groups processed");

            OrderAssert.InOrderExactlyOnce(processor.SuccessfulKeys(), groups.ToDictionary(group => group, _ => 3));

            Assert.Equal(0, aws.ThrottledVisibilityChanges);
            Assert.False(logger.Contains(Microsoft.Extensions.Logging.LogLevel.Warning, "could not be changed"));
        }

        [Fact]
        public async Task FailingGroupInAMixedReceive_IsRetried_WhileTheOtherGroupsAreDone()
        {
            var aws = new FakeAws();
            var sender = new SqsTestProcessor("S");
            var processor = new SqsTestProcessor("A")
            {
                Behaviour = call => call.Messages[0].Group == "B" && call.Attempt == 1 ? throw new InvalidOperationException("B fails once") : Task.CompletedTask
            };

            await using var scope = new MessengerScope();

            var senderMessenger = await scope.StartAsync(aws, aws, "S", sender, [new QueueSpec(_address, Listeners: 0, IsSession: true)]);

            for (var seq = 1; seq <= 2; seq++)
            {
                foreach (var group in new[] { "A", "B", "C" })
                {
                    await senderMessenger.SendMessageAsync(_address, new SqsTestMessage(group, seq), sessionId: group);
                }
            }

            await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec(_address, Listeners: 1, IsSession: true)]);

            await Wait.UntilAsync(() => processor.SuccessfulKeys().Count == 6, TimeSpan.FromSeconds(10), "all groups processed");

            var records = processor.Records.ToList();

            var failure = records.Single(record => record.Outcome != "ok");
            var bSuccess = records.Single(record => record.Outcome == "ok" && record.Keys[0] == "B#1");

            // B as a whole batch failed, A and C of the same receive were done without waiting for B's retry

            Assert.Equal(["B#1", "B#2"], failure.Keys);
            Assert.All(records.Where(record => record.Keys[0] != "B#1"), record => Assert.True(record.At < bSuccess.At));
            Assert.True(bSuccess.At - failure.At >= TimeSpan.FromSeconds(0.9), "B waits for the error hold");
            Assert.Equal(0, aws.ThrottledVisibilityChanges);

            OrderAssert.InOrderExactlyOnce(processor.SuccessfulKeys(), new Dictionary<string, int> { ["A"] = 2, ["B"] = 2, ["C"] = 2 });
        }

        [Fact]
        public async Task WaitingGroup_StaysWithTheListenerThatReceivedIt_WhileAnEarlierGroupIsProcessedLong()
        {
            var aws = new FakeAws();
            var sender = new SqsTestProcessor("S");
            var processingStarted = new TaskCompletionSource();

            var processorA = new SqsTestProcessor("A")
            {
                Behaviour = async call =>
                {
                    if (call.Messages[0].Group == "L")
                    {
                        processingStarted.TrySetResult();

                        // twice the visibility timeout: without the heartbeat for the waiting group, machine B would take it
                        await Task.Delay(TimeSpan.FromSeconds(4));
                    }
                }
            };

            var processorB = new SqsTestProcessor("B", processorA.Records);

            await using var scope = new MessengerScope();

            var senderMessenger = await scope.StartAsync(aws, aws, "S", sender, [new QueueSpec(_address, Listeners: 0, IsSession: true)]);

            await senderMessenger.SendMessageAsync(_address, new SqsTestMessage("L", 1), sessionId: "L");
            await senderMessenger.SendMessageAsync(_address, new SqsTestMessage("W", 1), sessionId: "W");

            await scope.StartAsync(aws, aws, "A", processorA, [new QueueSpec(_address, Listeners: 1, IsSession: true)]);

            await processingStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

            await scope.StartAsync(aws, aws, "B", processorB, [new QueueSpec(_address, Listeners: 2, IsSession: true)]);

            await Wait.UntilAsync(() => processorA.SuccessfulKeys().Count == 2, TimeSpan.FromSeconds(10), "both groups processed");

            await Task.Delay(TimeSpan.FromSeconds(1));

            var records = processorA.Records.ToList();

            Assert.Equal(2, records.Count);
            Assert.All(records, record => Assert.Equal("A", record.Machine));
            Assert.True(records.Single(record => record.Keys[0] == "W#1").At > records.Single(record => record.Keys[0] == "L#1").At);
        }

        [Fact]
        public async Task FinishedGroups_LeaveTheHeartbeat_WhileALaterGroupOfTheSameReceiveIsProcessed()
        {
            var aws = new FakeAws();
            var sender = new SqsTestProcessor("S");
            var logger = new TestLogger();

            var processor = new SqsTestProcessor("A")
            {
                Behaviour = call => call.Messages[0].Group switch
                {
                    "R" when call.Attempt == 1 => throw new RescheduleException(),
                    "P" when call.Attempt == 1 => throw new PostponeException(TimeSpan.FromSeconds(1)),
                    "L" => Task.Delay(TimeSpan.FromSeconds(2.5)),
                    _ => Task.CompletedTask
                }
            };

            var processorB = new SqsTestProcessor("B", processor.Records);

            await using var scope = new MessengerScope();

            var senderMessenger = await scope.StartAsync(aws, aws, "S", sender, [new QueueSpec(_address, Listeners: 0, IsSession: true)]);

            // one receive: D is done, R rescheduled, P postponed, then L runs for several heartbeats

            foreach (var group in new[] { "D", "R", "P", "L" })
            {
                await senderMessenger.SendMessageAsync(_address, new SqsTestMessage(group, 1), sessionId: group);
            }

            await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec(_address, Listeners: 1, IsSession: true)], logger: logger);
            await scope.StartAsync(aws, aws, "B", processorB, [new QueueSpec(_address, Listeners: 1, IsSession: true)]);

            await Wait.UntilAsync(() => processor.SuccessfulKeys().Count == 4, TimeSpan.FromSeconds(10), "all groups processed");

            var records = processor.Records.ToList();
            var lDone = records.Single(record => record.Keys[0] == "L#1").At;

            // R and P were released and held by the outcome alone: the other machine took them while L was still running,
            // and the heartbeat never touched a message that had left it

            Assert.True(records.Single(record => record.Outcome == "ok" && record.Keys[0] == "R#1").At < lDone);
            Assert.True(records.Single(record => record.Outcome == "ok" && record.Keys[0] == "P#1").At < lDone);
            Assert.False(logger.Contains(Microsoft.Extensions.Logging.LogLevel.Warning, "could not be changed"));
        }

        [Fact]
        public async Task ShutdownDuringAReceive_HandsTheGroupsNotStartedBack()
        {
            var aws = new FakeAws();
            var sender = new SqsTestProcessor("S");
            var processingStarted = new TaskCompletionSource();

            var processorA = new SqsTestProcessor("A")
            {
                Behaviour = async call =>
                {
                    if (call.Messages[0].Group == "L")
                    {
                        processingStarted.TrySetResult();

                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                }
            };

            var processorB = new SqsTestProcessor("B", processorA.Records);

            await using var scope = new MessengerScope();

            var senderMessenger = await scope.StartAsync(aws, aws, "S", sender, [new QueueSpec(_address, Listeners: 0, IsSession: true)]);

            foreach (var group in new[] { "L", "W", "X" })
            {
                await senderMessenger.SendMessageAsync(_address, new SqsTestMessage(group, 1), sessionId: group);
            }

            var messengerA = await scope.StartAsync(aws, aws, "A", processorA, [new QueueSpec(_address, Listeners: 1, IsSession: true)]);

            await processingStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

            await messengerA.ShutdownReceiversAsync();

            // the group in progress is completed; the two not started are visible again at once, not after the visibility timeout

            await Wait.UntilAsync(() => processorA.SuccessfulKeys().Count == 1, TimeSpan.FromSeconds(5), "group in progress completed");
            await Wait.UntilAsync(() => aws.GetMessages(_queueName).Count == 2 && aws.GetMessages(_queueName).All(message => !message.IsInFlight), TimeSpan.FromSeconds(1), "groups not started handed back");

            await scope.StartAsync(aws, aws, "B", processorB, [new QueueSpec(_address, Listeners: 1, IsSession: true)]);

            await Wait.UntilAsync(() => processorA.SuccessfulKeys().Count == 3, TimeSpan.FromSeconds(5), "groups not started processed by the other machine");

            Assert.Equal(["L#1"], processorA.Successes().Where(record => record.Machine == "A").SelectMany(record => record.Keys));
            Assert.Equal(["W#1", "X#1"], processorA.Successes().Where(record => record.Machine == "B").SelectMany(record => record.Keys).Order());
        }

        [Fact]
        public async Task FinalChangeRightAfterAHeartbeat_IsThrottled_AndSentAgain()
        {
            // a wider throttle window than SQS, so the release certainly falls into it after the heartbeat at 0.5 s

            var aws = new FakeAws { FifoVisibilityThrottleWindow = TimeSpan.FromMilliseconds(800) };
            var logger = new TestLogger();

            var processor = new SqsTestProcessor("A")
            {
                Behaviour = async call =>
                {
                    if (call.Attempt == 1)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(0.6));

                        throw new RescheduleException();
                    }
                }
            };

            await using var scope = new MessengerScope();

            var messenger = await scope.StartAsync(aws, aws, "A", processor, [new QueueSpec(_address, Listeners: 1, IsSession: true)], logger: logger);

            await messenger.SendMessageAsync(_address, new SqsTestMessage("G", 1), sessionId: "G");

            await Wait.UntilAsync(() => processor.Successes().Count == 1, TimeSpan.FromSeconds(10), "processed after the reschedule");

            var records = processor.Records.ToList();

            // released once the window has passed (about 1.3 s after the receive); without the retry the message would keep
            // the heartbeat's visibility of 2 s from 0.5 s on and follow 1.9 s after the reschedule

            Assert.True(aws.ThrottledVisibilityChanges > 0, "the release was throttled");
            Assert.True(records[1].At - records[0].At < TimeSpan.FromSeconds(1.4), $"the release was sent again once the window had passed ({records[1].At - records[0].At}, throttled {aws.ThrottledVisibilityChanges})");
            Assert.False(logger.Contains(Microsoft.Extensions.Logging.LogLevel.Warning, "could not be changed"));
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
