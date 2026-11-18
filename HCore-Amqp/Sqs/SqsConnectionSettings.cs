using System;
using Amazon;

namespace HCore.Amqp.Sqs
{
    // Amqp:ConnectionString for the SQS implementation, e.g. "Region=eu-central-1;AccessKeyId=..;SecretAccessKey=.." or
    // "Region=eu-central-1;AccessKeyId=..;SecretAccessKey=..;CreateResources=true;QueueNamePrefix=Staging" or
    // "Region=us-east-2;UseInstanceRole=true"

    internal class SqsConnectionSettings
    {
        internal const string DefaultRegion = "eu-central-1";

        internal string Region { get; private set; } = DefaultRegion;

        // the IAM user of the messenger; required unless UseInstanceRole is set

        internal string AccessKeyId { get; private set; }
        internal string SecretAccessKey { get; private set; }

        // uses the role of the EC2 instance instead of an IAM user, for hosts whose role grants the queue access;
        // never a profile or environment variables, so a machine without the setting cannot pick up other credentials

        internal bool UseInstanceRole { get; private set; }

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

                    case "UseInstanceRole":
                        if (!bool.TryParse(value, out var useInstanceRole))
                        {
                            throw new Exception($"SQS connection string value for UseInstanceRole is not a boolean: {value}");
                        }

                        connectionSettings.UseInstanceRole = useInstanceRole;
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

            var hasAccessKey = !string.IsNullOrEmpty(connectionSettings.AccessKeyId) || !string.IsNullOrEmpty(connectionSettings.SecretAccessKey);

            if (connectionSettings.UseInstanceRole)
            {
                if (hasAccessKey)
                {
                    throw new Exception("SQS connection string must not contain AccessKeyId or SecretAccessKey together with UseInstanceRole");
                }
            }
            else if (string.IsNullOrEmpty(connectionSettings.AccessKeyId) || string.IsNullOrEmpty(connectionSettings.SecretAccessKey))
            {
                throw new Exception("SQS connection string must contain AccessKeyId and SecretAccessKey, or UseInstanceRole=true");
            }

            if (!string.IsNullOrEmpty(queueNamePrefix))
            {
                connectionSettings.QueueNamePrefix = queueNamePrefix;
            }

            return connectionSettings;
        }
    }
}
