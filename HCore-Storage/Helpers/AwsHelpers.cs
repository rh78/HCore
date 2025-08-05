using System;
using System.Collections.Generic;
using Amazon;
using Amazon.S3;

namespace HCore.Storage.Helpers
{
    public static class AwsHelpers
    {
        public const string AccessKeyIdKey = "AccessKeyId";
        public const string SecretAccessKeyKey = "SecretAccessKey";

        public const string DefaultRegion = "eu-central-1";

        public static IAmazonS3 GetAmazonS3(IDictionary<string, string> connectionInfoByKey)
        {
            ArgumentNullException.ThrowIfNull(connectionInfoByKey);

            if (!connectionInfoByKey.TryGetValue(AccessKeyIdKey, out var accessKeyId))
            {
                throw new Exception("Missing access key");
            }

            if (!connectionInfoByKey.TryGetValue(SecretAccessKeyKey, out var secretAccessKey))
            {
                throw new Exception("Missing secret access key");
            }

            if (!connectionInfoByKey.TryGetValue("Region", out var region))
            {
                region = DefaultRegion;
            }

            var regionEndpoint = RegionEndpoint.GetBySystemName(region);

            return new AmazonS3Client(accessKeyId, secretAccessKey, regionEndpoint);
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
                    throw new Exception($"Connection string is invalid ({connectionString})");
                }

                connectionInfoByKey[parameterParts[0]] = parameterParts[1];
            }

            return connectionInfoByKey;
        }
    }
}
