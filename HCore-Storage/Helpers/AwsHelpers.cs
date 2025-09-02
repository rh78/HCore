using System;
using System.Collections.Generic;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace HCore.Storage.Helpers
{
    public static class AwsHelpers
    {
        public const string AccessKeyIdKey = "AccessKeyId";
        public const string SecretAccessKeyKey = "SecretAccessKey";
        public const string BucketNameKey = "BucketNameKey";

        public const string DefaultRegion = "eu-central-1";

        public static IAmazonS3 GetAmazonS3(IDictionary<string, string> connectionInfoByKey)
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
                    throw new ArgumentException($"Connection string is invalid ({connectionString})");
                }

                connectionInfoByKey[parameterParts[0]] = parameterParts[1];
            }

            return connectionInfoByKey;
        }

        public static string GetBucketName(IDictionary<string, string> connectionInfoByKey)
        {
            ArgumentNullException.ThrowIfNull(connectionInfoByKey);

            if (!connectionInfoByKey.TryGetValue(BucketNameKey, out var bucketNameKey) || string.IsNullOrEmpty(bucketNameKey))
            {
                throw new ArgumentException("Missing bucket name", BucketNameKey);
            }

            return bucketNameKey;
        }

        public static string GetAbsoluteUrl(IAmazonS3 amazonS3, string bucketName, string container, string fileName)
        {
            ArgumentNullException.ThrowIfNull(amazonS3);
            ArgumentNullException.ThrowIfNullOrEmpty(bucketName);
            ArgumentNullException.ThrowIfNullOrEmpty(fileName);

            var fileKey = GetFileKey(container, fileName);

            var endpoint = amazonS3.DetermineServiceOperationEndpoint(new GetObjectRequest
            {
                BucketName = bucketName,
                Key = fileKey
            });

            var uriBuilder = new UriBuilder(endpoint.URL)
            {
                Path = $"{container}/{Uri.EscapeDataString(fileName)}"
            };

            return uriBuilder.ToString();
        }

        public static string GetFileKey(string containerName, string fileName)
        {
            return $"{containerName}/{fileName}";
        }
    }
}
