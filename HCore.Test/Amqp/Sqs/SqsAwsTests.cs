using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using HCore.Amqp.Exceptions;
using HCore.Amqp.Sqs;
using Microsoft.Extensions.Logging;
using Xunit;

namespace HCore.Test.Amqp.Sqs
{
    // Runs the messenger against real SQS and SNS. The SQS connection string of the test IAM user is the secret
    // development/HCore-Test/Amqp/ConnectionString; the Secrets Manager access to read it comes from the git-ignored
    // HCore.Test/appsettings.Development.local.json ({ "SecretsManager": { "ConnectionString": "AccessKeyId=..;SecretAccessKey=.." } }).
    // Without that file the tests are skipped. Every test creates its own queues and topics under a unique prefix and
    // deletes them afterwards.

    public sealed class AwsFactAttribute : FactAttribute
    {
        public AwsFactAttribute()
        {
            if (!File.Exists(AwsTestSettings.LocalSettingsPath))
            {
                Skip = $"Create {AwsTestSettings.LocalSettingsFileName} with the Secrets Manager access to run the tests against AWS";
            }
        }
    }

    internal static class AwsTestSettings
    {
        internal const string LocalSettingsFileName = "appsettings.Development.local.json";

        private const string _secretId = "development/HCore-Test/Amqp/ConnectionString";

        internal static string LocalSettingsPath => Path.Combine(AppContext.BaseDirectory, LocalSettingsFileName);

        private static readonly Lazy<Task<string>> _connectionString = new(ReadConnectionStringAsync);

        internal static Task<string> GetConnectionStringAsync() => _connectionString.Value;

        private static async Task<string> ReadConnectionStringAsync()
        {
            using var localSettings = JsonDocument.Parse(await File.ReadAllTextAsync(LocalSettingsPath));

            var secretsManagerConnectionString = localSettings.RootElement.GetProperty("SecretsManager").GetProperty("ConnectionString").GetString();

            var connectionInfoByKey = secretsManagerConnectionString
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => part.Split('=', 2))
                .ToDictionary(parts => parts[0], parts => parts[1]);

            var region = RegionEndpoint.GetBySystemName(connectionInfoByKey.GetValueOrDefault("Region", "eu-central-1"));

            using var secretsManager = new AmazonSecretsManagerClient(new BasicAWSCredentials(connectionInfoByKey["AccessKeyId"], connectionInfoByKey["SecretAccessKey"]), region);

            var response = await secretsManager.GetSecretValueAsync(new GetSecretValueRequest { SecretId = _secretId });

            return response.SecretString;
        }
    }

    public class SqsAwsTests
    {
        private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);

        [AwsFact]
        public async Task StandardQueue_ProcessesDelaysReschedulesAndRetries()
        {
            await using var aws = await AwsTestEnvironment.CreateAsync();
            var logger = new TestLogger();

            var processor = new SqsTestProcessor("A")
            {
                Behaviour = call => call.Messages[0].Group switch
                {
                    "R" when call.Attempt == 1 => throw new RescheduleException(),
                    "E" when call.Attempt == 1 => throw new InvalidOperationException("first attempt fails"),
                    _ => Task.CompletedTask
                }
            };

            var messenger = await aws.StartAsync("A", processor, [new QueueSpec("Plain", Listeners: 2)], logger: logger);

            var sentAt = DateTimeOffset.UtcNow;

            await messenger.SendMessageAsync("Plain", new SqsTestMessage("N", 1));
            await messenger.SendMessageAsync("Plain", new SqsTestMessage("D", 1), timeOffsetSeconds: 4);
            await messenger.SendMessageAsync("Plain", new SqsTestMessage("R", 1));
            await messenger.SendMessageAsync("Plain", new SqsTestMessage("E", 1));

            await Wait.UntilAsync(() => processor.SuccessfulKeys().Count == 4, _timeout, "all messages processed");

            Assert.Equal(["D#1", "E#1", "N#1", "R#1"], processor.SuccessfulKeys().Order());
            Assert.True(processor.Successes().Single(record => record.Keys[0] == "D#1").At - sentAt >= TimeSpan.FromSeconds(3.9));

            var errorRecords = processor.Records.Where(record => record.Keys[0] == "E#1").ToList();

            Assert.True(errorRecords[1].At - errorRecords[0].At >= TimeSpan.FromSeconds(1.9));

            await aws.AssertQueueEmptiesAsync("Plain");

            Assert.False(logger.Contains(LogLevel.Error, "could not be deleted"));
        }

        [AwsFact]
        public async Task FifoQueue_KeepsOrderAcrossMachines_IsolatesAPostponingGroup_AndHoldsADelayedMessage()
        {
            await using var aws = await AwsTestEnvironment.CreateAsync();

            var longSessionId = $"{Guid.NewGuid()}_{Guid.NewGuid()}_{Guid.NewGuid()}_Context";

            Func<ProcessCall, Task> behaviour = async call =>
            {
                if (call.Messages[0].Group == "P")
                {
                    throw new PostponeException(TimeSpan.FromSeconds(2));
                }

                await Task.Delay(Random.Shared.Next(10, 100));
            };

            var processorA = new SqsTestProcessor("A") { Behaviour = behaviour };
            var processorB = new SqsTestProcessor("B", processorA.Records) { Behaviour = behaviour };

            var queues = new[] { new QueueSpec("Sessions", Listeners: 2, IsSession: true) };

            var messengerA = await aws.StartAsync("A", processorA, queues);
            await aws.StartAsync("B", processorB, queues);

            var sentAt = DateTimeOffset.UtcNow;

            await messengerA.SendMessageAsync("Sessions", new SqsTestMessage("D", 1), timeOffsetSeconds: 5, sessionId: "D");
            await messengerA.SendMessageAsync("Sessions", new SqsTestMessage("D", 2), sessionId: "D");

            for (var seq = 1; seq <= 5; seq++)
            {
                await messengerA.SendMessageAsync("Sessions", new SqsTestMessage("P", seq), sessionId: "P");
                await messengerA.SendMessageAsync("Sessions", new SqsTestMessage("L", seq), sessionId: longSessionId);

                foreach (var group in new[] { "G1", "G2", "G3" })
                {
                    await messengerA.SendMessageAsync("Sessions", new SqsTestMessage(group, seq), sessionId: group);
                }
            }

            await Wait.UntilAsync(() => processorA.SuccessfulKeys().Count == 22, _timeout, "all groups but P processed");

            OrderAssert.InOrderExactlyOnce(processorA.SuccessfulKeys(), new Dictionary<string, int> { ["G1"] = 5, ["G2"] = 5, ["G3"] = 5, ["L"] = 5, ["D"] = 2 });

            Assert.DoesNotContain(processorA.SuccessfulKeys(), key => key.StartsWith("P#"));
            Assert.True(processorA.Successes().Single(record => record.Keys.Contains("D#1")).At - sentAt >= TimeSpan.FromSeconds(4.9));
            Assert.Contains(processorA.Records, record => record.Machine == "A");
            Assert.Contains(processorA.Records, record => record.Machine == "B");
        }

        [AwsFact]
        public async Task LongProcessing_IsNotDeliveredToAnotherMachine()
        {
            await using var aws = await AwsTestEnvironment.CreateAsync();
            var logger = new TestLogger();

            var processorA = new SqsTestProcessor("A") { Behaviour = _ => Task.Delay(TimeSpan.FromSeconds(15)) };
            var processorB = new SqsTestProcessor("B", processorA.Records) { Behaviour = _ => Task.Delay(TimeSpan.FromSeconds(15)) };

            var queues = new[] { new QueueSpec("Plain"), new QueueSpec("Sessions", IsSession: true) };

            var messengerA = await aws.StartAsync("A", processorA, queues, logger: logger);
            await aws.StartAsync("B", processorB, queues, logger: logger);

            await messengerA.SendMessageAsync("Plain", new SqsTestMessage("S", 1));
            await messengerA.SendMessageAsync("Sessions", new SqsTestMessage("F", 1), sessionId: "F");

            await Wait.UntilAsync(() => processorA.SuccessfulKeys().Count == 2, _timeout, "long messages processed");

            await Task.Delay(TimeSpan.FromSeconds(7));

            Assert.Equal(2, processorA.Records.Count);

            await aws.AssertQueueEmptiesAsync("Plain");
            await aws.AssertQueueEmptiesAsync("Sessions.fifo");

            Assert.False(logger.Contains(LogLevel.Warning, "could not be changed"));
        }

        [AwsFact]
        public async Task BatchSend_DeliversEveryMessageOnce()
        {
            await using var aws = await AwsTestEnvironment.CreateAsync();

            var processor = new SqsTestProcessor("A");

            var messenger = await aws.StartAsync("A", processor, [new QueueSpec("Plain", Listeners: 4)]);

            await messenger.SendMessagesAsync("Plain", Enumerable.Range(1, 25).Select(seq => new SqsTestMessage("B", seq)).ToList());

            await Wait.UntilAsync(() => processor.SuccessfulKeys().Count == 25, _timeout, "batch processed");

            await Task.Delay(TimeSpan.FromSeconds(3));

            Assert.Equal(25, processor.SuccessfulKeys().Count);
            Assert.Equal(25, processor.SuccessfulKeys().Distinct().Count());
        }

        [AwsFact]
        public async Task Topic_ReachesEveryMachine_AndADelayedMessageIsHeld()
        {
            await using var aws = await AwsTestEnvironment.CreateAsync();

            var processorA = new SqsTestProcessor("A");
            var processorB = new SqsTestProcessor("B", processorA.Records);

            var queues = new[] { new QueueSpec("Plain", Listeners: 0) };
            var topics = new[] { new TopicSpec("Cache") };

            var messengerA = await aws.StartAsync("A", processorA, queues, topics);
            await aws.StartAsync("B", processorB, queues, topics);

            await messengerA.SendMessageAsync("Cache", new SqsTestMessage("T", 1));

            await Wait.UntilAsync(() => processorA.Successes("Cache").Count == 2, _timeout, "both machines processed the topic message");

            var sentAt = DateTimeOffset.UtcNow;

            await messengerA.SendMessageAsync("Cache", new SqsTestMessage("T", 2), timeOffsetSeconds: 4);

            await Wait.UntilAsync(() => processorA.Successes("Cache").Count == 4, _timeout, "both machines processed the delayed topic message");

            var delayedRecords = processorA.Successes("Cache").Where(record => record.Keys[0] == "T#2").ToList();

            Assert.Equal(["A", "B"], delayedRecords.Select(record => record.Machine).Order());
            Assert.All(delayedRecords, record => Assert.True(record.At - sentAt >= TimeSpan.FromSeconds(3.9)));
        }

        [AwsFact]
        public async Task MaxReceiveCount_MovesAFailingMessageToTheDeadLetterQueue()
        {
            await using var aws = await AwsTestEnvironment.CreateAsync();

            var processor = new SqsTestProcessor("A") { Behaviour = _ => throw new InvalidOperationException("always") };

            var messenger = await aws.StartAsync("A", processor, [new QueueSpec("Plain", MaxReceiveCount: 2)]);

            await messenger.SendMessageAsync("Plain", new SqsTestMessage("X", 1));

            await aws.WaitForMessageAsync("Plain-dlq", _timeout);

            Assert.Equal(2, processor.Records.Count);
        }

        [AwsFact]
        public async Task WithoutCreateResources_UsesTheExistingQueuesAndTopics()
        {
            await using var aws = await AwsTestEnvironment.CreateAsync();

            var processor = new SqsTestProcessor("A");

            var queues = new[] { new QueueSpec("Plain"), new QueueSpec("Sessions", IsSession: true) };
            var topics = new[] { new TopicSpec("Cache") };

            // the first start creates the resources, as Terraform would

            var creatingMessenger = await aws.StartAsync("A", new SqsTestProcessor("setup"), queues, topics);

            await creatingMessenger.ShutdownReceiversAsync();

            var messenger = await aws.StartAsync("A", processor, queues, topics, createResources: false);

            await messenger.SendMessageAsync("Plain", new SqsTestMessage("P", 1));
            await messenger.SendMessageAsync("Sessions", new SqsTestMessage("S", 1), sessionId: "S");
            await messenger.SendMessageAsync("Cache", new SqsTestMessage("T", 1));

            await Wait.UntilAsync(() => processor.SuccessfulKeys().Count == 3, _timeout, "all addresses processed");

            Assert.Equal(["P#1", "S#1", "T#1"], processor.SuccessfulKeys().Order());

            var missing = await Assert.ThrowsAsync<Exception>(() => aws.StartAsync("A", processor, [new QueueSpec("Missing")], createResources: false));

            Assert.Contains("does not exist", missing.Message);
        }
    }

    internal sealed class AwsTestEnvironment : IAsyncDisposable
    {
        private readonly MessengerScope _scope = new();

        private readonly string _connectionString;
        private readonly string _prefix;

        private readonly AwsMessagingApi _awsMessagingApi;

        private readonly AmazonSQSClient _sqsClient;
        private readonly AmazonSimpleNotificationServiceClient _snsClient;

        private readonly string _region;

        private readonly HashSet<string> _queueNames = [];
        private readonly HashSet<string> _topicNames = [];

        private AwsTestEnvironment(string connectionString)
        {
            _connectionString = connectionString;

            _prefix = $"hcoretest-{Guid.NewGuid():N}"[..20] + "-";

            var connectionSettings = SqsConnectionSettings.Parse(_connectionString);

            _region = connectionSettings.Region;

            _awsMessagingApi = new AwsMessagingApi(connectionSettings);

            var regionEndpoint = RegionEndpoint.GetBySystemName(connectionSettings.Region);
            var credentials = new BasicAWSCredentials(connectionSettings.AccessKeyId, connectionSettings.SecretAccessKey);

            _sqsClient = new AmazonSQSClient(credentials, regionEndpoint);
            _snsClient = new AmazonSimpleNotificationServiceClient(credentials, regionEndpoint);
        }

        internal static async Task<AwsTestEnvironment> CreateAsync()
        {
            return new AwsTestEnvironment(await AwsTestSettings.GetConnectionStringAsync());
        }

        internal Task<HCore.Amqp.Messenger.Impl.SqsMessengerImpl> StartAsync(string machine, SqsTestProcessor processor, QueueSpec[] queues, TopicSpec[] topics = null, TestLogger logger = null, bool createResources = true)
        {
            // ListQueues shows a new queue only after a while, so the cleanup also deletes the queues by name

            foreach (var queue in queues)
            {
                var queueName = SqsNames.GetQueueName(_prefix, queue.Address, queue.IsSession);

                _queueNames.Add(queueName);
                _queueNames.Add(SqsNames.GetDeadLetterQueueName(queueName));
            }

            foreach (var topic in topics ?? [])
            {
                _queueNames.Add(SqsNames.GetTopicQueueName(_prefix, topic.Address, machine));
                _topicNames.Add(SqsNames.GetTopicName(_prefix, topic.Address));
            }

            return _scope.StartAsync(_awsMessagingApi, _awsMessagingApi, machine, processor, queues, topics,
                connectionString: $"{_connectionString};CreateResources={createResources};QueueNamePrefix={_prefix}",
                timings: new SqsTimings
                {
                    VisibilityTimeout = TimeSpan.FromSeconds(5),
                    HeartbeatInterval = TimeSpan.FromSeconds(1),
                    ErrorHold = TimeSpan.FromSeconds(2),
                    FifoPostponeHold = TimeSpan.FromSeconds(2),
                    StandardPostponeHold = TimeSpan.FromSeconds(2),
                    ReceiveWaitTimeSeconds = 1,
                    RetryDelay = TimeSpan.FromMilliseconds(500)
                },
                logger: logger);
        }

        internal async Task AssertQueueEmptiesAsync(string queueName)
        {
            var queueUrl = (await _sqsClient.GetQueueUrlAsync(_prefix + queueName)).QueueUrl;

            var deadline = DateTimeOffset.UtcNow.AddSeconds(90);

            while (true)
            {
                var attributes = await _sqsClient.GetQueueAttributesAsync(queueUrl, ["ApproximateNumberOfMessages", "ApproximateNumberOfMessagesNotVisible", "ApproximateNumberOfMessagesDelayed"]);

                if (attributes.Attributes.Values.All(value => value == "0"))
                {
                    return;
                }

                if (DateTimeOffset.UtcNow > deadline)
                {
                    Assert.Fail($"Queue {queueName} still holds messages: {string.Join(", ", attributes.Attributes.Select(attribute => $"{attribute.Key}={attribute.Value}"))}");
                }

                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }

        internal async Task WaitForMessageAsync(string queueName, TimeSpan timeout)
        {
            var queueUrl = (await _sqsClient.GetQueueUrlAsync(_prefix + queueName)).QueueUrl;

            var deadline = DateTimeOffset.UtcNow + timeout;

            while (DateTimeOffset.UtcNow < deadline)
            {
                var response = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest(queueUrl) { WaitTimeSeconds = 5 });

                if (response.Messages?.Count > 0)
                {
                    return;
                }
            }

            Assert.Fail($"No message arrived on {queueName}");
        }

        public async ValueTask DisposeAsync()
        {
            await _scope.DisposeAsync();

            _awsMessagingApi.Dispose();

            var queueUrls = new HashSet<string>((await _sqsClient.ListQueuesAsync(new ListQueuesRequest { QueueNamePrefix = _prefix })).QueueUrls ?? []);

            foreach (var queueName in _queueNames)
            {
                try
                {
                    queueUrls.Add((await _sqsClient.GetQueueUrlAsync(queueName)).QueueUrl);
                }
                catch (QueueDoesNotExistException)
                {
                    // not created by this test
                }
            }

            foreach (var queueUrl in queueUrls)
            {
                await _sqsClient.DeleteQueueAsync(queueUrl);
            }

            var topicArns = new HashSet<string>();

            if (queueUrls.Count > 0)
            {
                var accountId = SqsNames.GetAccountId(queueUrls.First());

                topicArns.UnionWith(_topicNames.Select(topicName => SqsNames.GetTopicArn(_region, accountId, topicName)));
            }

            string nextToken = null;

            do
            {
                var response = await _snsClient.ListTopicsAsync(nextToken);

                topicArns.UnionWith((response.Topics ?? []).Select(topic => topic.TopicArn).Where(topicArn => topicArn.Contains($":{_prefix}")));

                nextToken = response.NextToken;
            }
            while (!string.IsNullOrEmpty(nextToken));

            // deleting a topic that does not exist succeeds

            foreach (var topicArn in topicArns)
            {
                await _snsClient.DeleteTopicAsync(topicArn);
            }

            _sqsClient.Dispose();
            _snsClient.Dispose();
        }
    }
}
