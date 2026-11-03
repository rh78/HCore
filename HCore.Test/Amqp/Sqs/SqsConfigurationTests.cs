using System;
using HCore.Amqp.Processor.Hosts;
using HCore.Amqp.Sqs;
using Xunit;

namespace HCore.Test.Amqp.Sqs
{
    public class SqsConfigurationTests
    {
        [Fact]
        public void ConnectionString_WithRegionAndKeys_CreatesNothing()
        {
            var connectionSettings = SqsConnectionSettings.Parse("Region=eu-west-1;AccessKeyId=AKIA123;SecretAccessKey=secret");

            Assert.Equal("eu-west-1", connectionSettings.Region);
            Assert.Equal("AKIA123", connectionSettings.AccessKeyId);
            Assert.Equal("secret", connectionSettings.SecretAccessKey);
            Assert.Null(connectionSettings.ServiceUrl);
            Assert.False(connectionSettings.CreateResources);
            Assert.Equal(string.Empty, connectionSettings.QueueNamePrefix);
        }

        [Fact]
        public void ConnectionString_WithAllKeys_IsParsed()
        {
            var connectionSettings = SqsConnectionSettings.Parse("Region=us-east-1; AccessKeyId=AKIA123 ;SecretAccessKey=se/cr+et;ServiceUrl=http://localhost:4566;CreateResources=true;QueueNamePrefix=Staging-;");

            Assert.Equal("us-east-1", connectionSettings.Region);
            Assert.Equal("AKIA123", connectionSettings.AccessKeyId);
            Assert.Equal("se/cr+et", connectionSettings.SecretAccessKey);
            Assert.Equal("http://localhost:4566", connectionSettings.ServiceUrl);
            Assert.True(connectionSettings.CreateResources);
            Assert.Equal("Staging-", connectionSettings.QueueNamePrefix);
        }

        [Fact]
        public void SeparateQueueNamePrefix_WinsOverTheConnectionString()
        {
            Assert.Equal("dev-rh-", SqsConnectionSettings.Parse("AccessKeyId=AKIA123;SecretAccessKey=secret;QueueNamePrefix=dev-", "dev-rh-").QueueNamePrefix);
            Assert.Equal("dev-rh-", SqsConnectionSettings.Parse("AccessKeyId=AKIA123;SecretAccessKey=secret", "dev-rh-").QueueNamePrefix);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void WithoutSeparateQueueNamePrefix_TheConnectionStringPrefixApplies(string queueNamePrefix)
        {
            Assert.Equal("dev-", SqsConnectionSettings.Parse("AccessKeyId=AKIA123;SecretAccessKey=secret;QueueNamePrefix=dev-", queueNamePrefix).QueueNamePrefix);
        }

        [Fact]
        public void ConnectionString_WithoutRegion_UsesTheDefaultRegion()
        {
            Assert.Equal("eu-central-1", SqsConnectionSettings.Parse("AccessKeyId=AKIA123;SecretAccessKey=secret").Region);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Region")]
        [InlineData("Region=eu-central-1;Unknown=1")]
        [InlineData("CreateResources=yes")]
        [InlineData("AccessKeyId=AKIA123")]
        [InlineData("SecretAccessKey=secret")]
        [InlineData("Region=eu-central-1")]
        [InlineData("Region=eu-central-1;CreateResources=true")]
        [InlineData("tcp://user:password@broker:61616")]
        public void ConnectionString_Invalid_IsRejected(string connectionString)
        {
            Assert.Throws<Exception>(() => SqsConnectionSettings.Parse(connectionString));
        }

        [Fact]
        public void QueueNames_FollowTheAddress_WithFifoSuffixForSessions()
        {
            Assert.Equal("SmintIoIntegrationLayerTasks.fifo", SqsNames.GetQueueName("", "SmintIoIntegrationLayerTasks", isFifo: true));
            Assert.Equal("Prod-SmintIoEmailSenderTasks", SqsNames.GetQueueName("Prod-", "SmintIoEmailSenderTasks", isFifo: false));
            Assert.Equal("SmintIoIntegrationLayerTasks-dlq.fifo", SqsNames.GetDeadLetterQueueName("SmintIoIntegrationLayerTasks.fifo"));
            Assert.Equal("SmintIoEmailSenderTasks-dlq", SqsNames.GetDeadLetterQueueName("SmintIoEmailSenderTasks"));
        }

        [Fact]
        public void TopicQueueName_ContainsTheSanitizedMachineName_AndStaysWithinTheLimit()
        {
            Assert.Equal("SmintIoPortalsApiBMemoryCacheHandlerTasks-ip-10-0-1-5-eu-central-1-compute", SqsNames.GetTopicQueueName("", "SmintIoPortalsApiBMemoryCacheHandlerTasks", "ip-10-0-1-5.eu-central-1.compute"));

            var longName = SqsNames.GetTopicQueueName("Production-", "SmintIoPortalsApiBComponentUpdateHandlerTasks", "a-very-long-machine-name-that-goes-on");

            Assert.Equal(80, longName.Length);
        }

        [Fact]
        public void TopicArn_IsBuiltFromTheAccountOfAQueueUrl()
        {
            var accountId = SqsNames.GetAccountId("https://sqs.eu-central-1.amazonaws.com/123456789012/SmintIoEmailSenderTasks");

            Assert.Equal("123456789012", accountId);
            Assert.Equal("arn:aws:sns:eu-central-1:123456789012:Topic", SqsNames.GetTopicArn("eu-central-1", accountId, "Topic"));
            Assert.Equal("arn:aws-cn:sns:cn-north-1:123456789012:Topic", SqsNames.GetTopicArn("cn-north-1", accountId, "Topic"));
            Assert.Throws<Exception>(() => SqsNames.GetAccountId("http://localhost:4566/queue"));
        }

        [Theory]
        [InlineData("2f8c3a1e-1111-2222-3333-444455556666")]
        [InlineData("dev_tenant_portal_context")]
        public void MessageGroupId_KeepsAValidSessionId(string sessionId)
        {
            Assert.Equal(sessionId, SqsQueueHost.GetMessageGroupId(sessionId));
        }

        [Fact]
        public void MessageGroupId_HashesASessionIdSqsWouldReject()
        {
            var longSessionId = new string('a', 129);

            Assert.Equal(64, SqsQueueHost.GetMessageGroupId(longSessionId).Length);
            Assert.Equal(SqsQueueHost.GetMessageGroupId(longSessionId), SqsQueueHost.GetMessageGroupId(longSessionId));
            Assert.NotEqual(SqsQueueHost.GetMessageGroupId(longSessionId), SqsQueueHost.GetMessageGroupId(new string('b', 129)));
            Assert.Equal(64, SqsQueueHost.GetMessageGroupId("with space").Length);
        }
    }
}
