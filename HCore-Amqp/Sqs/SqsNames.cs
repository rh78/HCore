using System;
using System.Linq;
using System.Text;
using Amazon;

namespace HCore.Amqp.Sqs
{
    internal static class SqsNames
    {
        internal const string FifoSuffix = ".fifo";
        internal const string DeadLetterQueueSuffix = "-dlq";

        // SQS queue names: up to 80 characters, letters, digits, hyphens and underscores, plus ".fifo"

        private const int _maximumQueueNameLength = 80;

        internal static string GetQueueName(string queueNamePrefix, string address, bool isFifo)
        {
            return $"{queueNamePrefix}{address}{(isFifo ? FifoSuffix : string.Empty)}";
        }

        internal static string GetDeadLetterQueueName(string queueName)
        {
            if (queueName.EndsWith(FifoSuffix, StringComparison.Ordinal))
            {
                return $"{queueName[..^FifoSuffix.Length]}{DeadLetterQueueSuffix}{FifoSuffix}";
            }

            return $"{queueName}{DeadLetterQueueSuffix}";
        }

        internal static string GetTopicName(string queueNamePrefix, string topicAddress)
        {
            return $"{queueNamePrefix}{topicAddress}";
        }

        // every instance receives every topic message through its own queue, like the Service Bus subscription per machine

        internal static string GetTopicQueueName(string queueNamePrefix, string topicAddress, string machineName)
        {
            var queueName = $"{queueNamePrefix}{topicAddress}-{SanitizeName(machineName)}";

            return queueName.Length > _maximumQueueNameLength
                ? queueName[.._maximumQueueNameLength]
                : queueName;
        }

        private static string SanitizeName(string name)
        {
            var sanitizedName = new StringBuilder(name.Length);

            foreach (var character in name)
            {
                sanitizedName.Append(char.IsAsciiLetterOrDigit(character) || character == '-' || character == '_' ? character : '-');
            }

            return sanitizedName.ToString();
        }

        // the queue URL ends in /<account ID>/<queue name>

        internal static string GetAccountId(string queueUrl)
        {
            var pathSegments = new Uri(queueUrl).AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (pathSegments.Length < 2 || !pathSegments[^2].All(char.IsAsciiDigit))
            {
                throw new Exception($"SQS queue URL does not contain an account ID: {queueUrl}");
            }

            return pathSegments[^2];
        }

        internal static string GetTopicArn(string region, string accountId, string topicName)
        {
            var partitionName = RegionEndpoint.GetBySystemName(region).PartitionName ?? "aws";

            return $"arn:{partitionName}:sns:{region}:{accountId}:{topicName}";
        }
    }
}
