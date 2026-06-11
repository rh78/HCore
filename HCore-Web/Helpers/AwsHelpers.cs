using System;
using System.Collections.Generic;
using Amazon;
using Amazon.SecretsManager;

namespace HCore.Storage.Helpers
{
    internal static class AwsHelpers
    {
        public const string AccessKeyIdKey = "AccessKeyId";
        public const string SecretAccessKeyKey = "SecretAccessKey";

        public const string DefaultRegion = "eu-central-1";

        public static IAmazonSecretsManager GetSecretsManager(IDictionary<string, string> connectionInfoByKey)
        {
            ArgumentNullException.ThrowIfNull(connectionInfoByKey);

            if (!connectionInfoByKey.TryGetValue(AccessKeyIdKey, out var accessKeyId))
            {
                throw new ArgumentException("Missing access key", AccessKeyIdKey);
            }

            if (!connectionInfoByKey.TryGetValue(SecretAccessKeyKey, out var secretAccessKey))
            {
                throw new ArgumentException("Missing secret access key", SecretAccessKeyKey);
            }

            if (!connectionInfoByKey.TryGetValue("Region", out var region))
            {
                region = DefaultRegion;
            }

            var regionEndpoint = RegionEndpoint.GetBySystemName(region);

            if (regionEndpoint == null)
            {
                throw new ArgumentException($"Invalid AWS region: {region}", "Region");
            }

            return new AmazonSecretsManagerClient(accessKeyId, secretAccessKey, regionEndpoint);
        }

        public static IDictionary<string, string> GetConnectionInfoByKey(string connectionString)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(connectionString);

            var connectionInfoByKey = new Dictionary<string, string>();

            var connectionStringParts = connectionString.Split(";", StringSplitOptions.RemoveEmptyEntries);

            if (connectionStringParts.Length == 0)
            {
                return connectionInfoByKey;
            }

            foreach (var connectionStringPart in connectionStringParts)
            {
                if (string.IsNullOrEmpty(connectionStringPart))
                    continue;

                var parameterParts = connectionStringPart.Split("=", StringSplitOptions.RemoveEmptyEntries);

                if (parameterParts.Length != 2)
                {
                    throw new ArgumentException($"Connection string is invalid ({connectionString})");
                }

                connectionInfoByKey[parameterParts[0]] = parameterParts[1];
            }

            return connectionInfoByKey;
        }
    }
}
