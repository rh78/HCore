using System;
using Amazon;

namespace HCore.Amqp.Sqs
{
    // Amqp:ConnectionString for the SQS implementation, e.g. "Region=eu-central-1;AccessKeyId=..;SecretAccessKey=.." or
    // "Region=eu-central-1;AccessKeyId=..;SecretAccessKey=..;CreateResources=true;QueueNamePrefix=Staging"

    internal class SqsConnectionSettings
    {
        internal const string DefaultRegion = "eu-central-1";

        internal string Region { get; private set; } = DefaultRegion;

        // required: the messenger uses an IAM user, the machine identity only gives access to Secrets Manager

        internal string AccessKeyId { get; private set; }
        internal string SecretAccessKey { get; private set; }

        // optional endpoint instead of the regional AWS endpoint

        internal string ServiceUrl { get; private set; }

        // creates missing queues, dead-letter queues, topics and topic subscriptions at startup;
        // otherwise all of them must exist (e.g. created by Terraform)

        internal bool CreateResources { get; private set; }

        internal string QueueNamePrefix { get; private set; } = string.Empty;

        // a queue name prefix given separately (Amqp:QueueNamePrefix) wins over the one in the connection string

        internal static SqsConnectionSettings Parse(string connectionString, string queueNamePrefix = null)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("SQS connection string is empty");
            }

            var connectionSettings = new SqsConnectionSettings();

            foreach (var connectionStringPart in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var separatorIndex = connectionStringPart.IndexOf('=');

                if (separatorIndex <= 0)
                {
                    throw new Exception("SQS connection string is invalid, every part must be key=value");
                }

                var key = connectionStringPart[..separatorIndex].Trim();
                var value = connectionStringPart[(separatorIndex + 1)..].Trim();

                switch (key)
                {
                    case "Region":
                        connectionSettings.Region = value;
                        break;

                    case "AccessKeyId":
                        connectionSettings.AccessKeyId = value;
                        break;

                    case "SecretAccessKey":
                        connectionSettings.SecretAccessKey = value;
                        break;

                    case "ServiceUrl":
                        connectionSettings.ServiceUrl = value;
                        break;

                    case "CreateResources":
                        if (!bool.TryParse(value, out var createResources))
                        {
                            throw new Exception($"SQS connection string value for CreateResources is not a boolean: {value}");
                        }

                        connectionSettings.CreateResources = createResources;
                        break;

                    case "QueueNamePrefix":
                        connectionSettings.QueueNamePrefix = value;
                        break;

                    default:
                        throw new Exception($"SQS connection string contains the unknown key {key}");
                }
            }

            if (string.IsNullOrEmpty(connectionSettings.Region) || RegionEndpoint.GetBySystemName(connectionSettings.Region) == null)
            {
                throw new Exception($"SQS region is invalid: {connectionSettings.Region}");
            }

            if (string.IsNullOrEmpty(connectionSettings.AccessKeyId) || string.IsNullOrEmpty(connectionSettings.SecretAccessKey))
            {
                throw new Exception("SQS connection string must contain AccessKeyId and SecretAccessKey");
            }

            if (!string.IsNullOrEmpty(queueNamePrefix))
            {
                connectionSettings.QueueNamePrefix = queueNamePrefix;
            }

            return connectionSettings;
        }
    }
}
