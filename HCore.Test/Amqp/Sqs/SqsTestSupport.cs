using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCore.Amqp.Message;
using HCore.Amqp.Messenger.Impl;
using HCore.Amqp.Processor;
using HCore.Amqp.Sqs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;

namespace HCore.Test.Amqp.Sqs
{
    internal class SqsTestMessage : AMQPMessage
    {
        public string Group { get; set; }

        public int Seq { get; set; }

        public string Payload { get; set; }

        public SqsTestMessage(string group, int seq, string payload = null)
            : base("test")
        {
            Group = group;
            Seq = seq;
            Payload = payload;
        }

        public override string ToString() => $"{Group}#{Seq}";
    }

    internal record ProcessCall(string Machine, string Address, List<SqsTestMessage> Messages, bool IsBatch, int Attempt, DateTimeOffset At);

    internal record ProcessRecord(string Machine, string Address, List<string> Keys, bool IsBatch, string Outcome, DateTimeOffset At);

    // records every call; Behaviour decides per call whether it succeeds or throws

    internal class SqsTestProcessor : IAMQPMessageProcessor
    {
        private readonly string _machine;

        private readonly ConcurrentDictionary<string, int> _attemptsByKey;

        internal ConcurrentQueue<ProcessRecord> Records { get; }

        internal Func<ProcessCall, Task> Behaviour { get; set; } = _ => Task.CompletedTask;

        internal SqsTestProcessor(string machine, ConcurrentQueue<ProcessRecord> records = null, ConcurrentDictionary<string, int> attemptsByKey = null)
        {
            _machine = machine;

            Records = records ?? new ConcurrentQueue<ProcessRecord>();

            _attemptsByKey = attemptsByKey ?? new ConcurrentDictionary<string, int>();
        }

        public Task<bool> ProcessMessageAsync(string address, string messageBodyJson)
        {
            return ProcessAsync(address, [messageBodyJson], isBatch: false);
        }

        public Task<bool> ProcessMessagesAsync(string address, List<string> messageBodyJsons)
        {
            return ProcessAsync(address, messageBodyJsons, isBatch: true);
        }

        private async Task<bool> ProcessAsync(string address, List<string> messageBodyJsons, bool isBatch)
        {
            var messages = messageBodyJsons.Select(messageBodyJson => JsonConvert.DeserializeObject<SqsTestMessage>(messageBodyJson)).ToList();
            var keys = messages.Select(message => message.ToString()).ToList();

            var attempt = _attemptsByKey.AddOrUpdate(keys[0], 1, (_, count) => count + 1);

            try
            {
                await Behaviour(new ProcessCall(_machine, address, messages, isBatch, attempt, DateTimeOffset.UtcNow)).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Records.Enqueue(new ProcessRecord(_machine, address, keys, isBatch, exception.GetType().Name, DateTimeOffset.UtcNow));

                throw;
            }

            Records.Enqueue(new ProcessRecord(_machine, address, keys, isBatch, "ok", DateTimeOffset.UtcNow));

            return true;
        }

        internal List<ProcessRecord> Successes(string address = null) =>
            Records.Where(record => record.Outcome == "ok" && (address == null || record.Address == address)).ToList();

        internal List<string> SuccessfulKeys(string address = null) =>
            Successes(address).SelectMany(record => record.Keys).ToList();
    }

    internal class TestLogger : ILogger<SqsMessengerImpl>
    {
        internal ConcurrentQueue<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Entries.Enqueue((logLevel, formatter(state, exception)));
        }

        internal bool Contains(LogLevel level, string text) => Entries.Any(entry => entry.Level == level && entry.Message.Contains(text));
    }

    internal record QueueSpec(string Address, int Listeners = 1, bool IsSession = false, int? MaxReceiveCount = null);

    internal record TopicSpec(string Address, int Listeners = 1);

    // creates messengers ("machines") against one SQS/SNS and shuts all of them down at the end

    internal sealed class MessengerScope : IAsyncDisposable
    {
        // placeholders: the fake AWS checks no credentials, but the connection string requires them

        internal const string TestCredentials = "Region=eu-central-1;AccessKeyId=test;SecretAccessKey=test";

        private readonly List<SqsMessengerImpl> _messengers = [];

        internal static SqsTimings FastTimings => new()
        {
            VisibilityTimeout = TimeSpan.FromSeconds(2),
            HeartbeatInterval = TimeSpan.FromMilliseconds(500),
            ErrorHold = TimeSpan.FromSeconds(1),
            FifoPostponeHold = TimeSpan.FromSeconds(1),
            StandardPostponeHold = TimeSpan.FromSeconds(1),
            ReceiveWaitTimeSeconds = 1,
            RetryDelay = TimeSpan.FromMilliseconds(100)
        };

        internal async Task<SqsMessengerImpl> StartAsync(
            ISqsApi sqsApi,
            ISnsApi snsApi,
            string machine,
            IAMQPMessageProcessor processor,
            QueueSpec[] queues,
            TopicSpec[] topics = null,
            string connectionString = TestCredentials + ";CreateResources=true",
            SqsTimings timings = null,
            TestLogger logger = null)
        {
            topics ??= [];

            var messenger = new SqsMessengerImpl(
                SqsConnectionSettings.Parse(connectionString),
                sqsApi,
                snsApi,
                timings ?? FastTimings,
                machine,
                queues.Select(queue => queue.Address).ToArray(),
                topics.Select(topic => topic.Address).ToArray(),
                queues.Select(queue => queue.Listeners).ToArray(),
                topics.Select(topic => topic.Listeners).ToArray(),
                queues.Select(queue => queue.IsSession).ToArray(),
                topics.Select(_ => false).ToArray(),
                queues.Select(queue => queue.MaxReceiveCount).ToArray(),
                processor,
                logger ?? new TestLogger());

            _messengers.Add(messenger);

            await messenger.InitializeAsync();

            return messenger;
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var messenger in _messengers)
            {
                await messenger.ShutdownReceiversAsync();
            }

            foreach (var messenger in _messengers)
            {
                await messenger.ShutdownAsync();
            }
        }
    }

    internal static class Wait
    {
        internal static async Task UntilAsync(Func<bool> condition, TimeSpan timeout, string description)
        {
            var deadline = DateTimeOffset.UtcNow + timeout;

            while (!condition())
            {
                if (DateTimeOffset.UtcNow > deadline)
                {
                    Assert.Fail($"Timed out after {timeout} waiting for: {description}");
                }

                await Task.Delay(50);
            }
        }
    }

    internal static class OrderAssert
    {
        // every group was processed completely, in order, and each message exactly once

        internal static void InOrderExactlyOnce(IEnumerable<string> successfulKeys, IDictionary<string, int> expectedCountsByGroup)
        {
            var keysByGroup = successfulKeys
                .Select(key => key.Split('#'))
                .GroupBy(parts => parts[0], parts => int.Parse(parts[1]))
                .ToDictionary(group => group.Key, group => group.ToList());

            foreach (var (group, expectedCount) in expectedCountsByGroup)
            {
                Assert.True(keysByGroup.TryGetValue(group, out var sequence), $"group {group} was not processed");
                Assert.Equal(Enumerable.Range(1, expectedCount).ToList(), sequence);
            }
        }
    }
}
