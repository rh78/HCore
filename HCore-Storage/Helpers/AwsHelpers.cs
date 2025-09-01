using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using HCore.Storage.Models.AwsStorage;
using Newtonsoft.Json;

namespace HCore.Storage.Helpers
{
    public static class AwsHelpers
    {
        public const string AccessKeyIdKey = "AccessKeyId";
        public const string SecretAccessKeyKey = "SecretAccessKey";
        public const string BucketNameKey = "BucketNameKey";

        public const string DefaultRegion = "eu-central-1";

        private static readonly ICollection<string> _publicFolders =
        [
            "smintiof",
            "smintiopcce",
            "smintiof",
            "smintiof",
            "smintiopfepcdnrp"
        ];

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

        public static async Task CreateContainerAsync(IAmazonS3 amazonS3, string bucketName, string container, bool isPublic)
        {
            ArgumentNullException.ThrowIfNull(amazonS3);
            ArgumentNullException.ThrowIfNullOrEmpty(bucketName);
            ArgumentNullException.ThrowIfNullOrEmpty(container);

            GetBucketLocationResponse getBucketLocationResponse = null;

            var isNew = false;

            try
            {
                getBucketLocationResponse = await amazonS3.GetBucketLocationAsync(bucketName).ConfigureAwait(false);
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.StatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }

                isNew = true;
            }

            if (getBucketLocationResponse == null)
            {
                var putBucketRequest = new PutBucketRequest
                {
                    BucketName = bucketName
                };

                await amazonS3.PutBucketAsync(putBucketRequest).ConfigureAwait(false);

                // https://docs.aws.amazon.com/AmazonS3/latest/userguide/access-control-block-public-access.html

                var putPublicAccessBlockRequest = new PutPublicAccessBlockRequest
                {
                    BucketName = bucketName,
                    PublicAccessBlockConfiguration = new PublicAccessBlockConfiguration
                    {
                        BlockPublicAcls = false,
                        IgnorePublicAcls = false,
                        BlockPublicPolicy = false,
                        RestrictPublicBuckets = false
                    }
                };

                await amazonS3.PutPublicAccessBlockAsync(putPublicAccessBlockRequest).ConfigureAwait(false);
            }

            if (isNew || isPublic)
            {
                // https://docs.aws.amazon.com/AmazonS3/latest/userguide/WebsiteAccessPermissionsReqd.html

                var resources = _publicFolders
                    .Select(publicFolder => $"arn:aws:s3:::{bucketName}/{publicFolder}*")
                    .ToList();

                if (!_publicFolders.Any(publicFolder => container.StartsWith(publicFolder, StringComparison.OrdinalIgnoreCase)))
                {
                    resources.Add($"arn:aws:s3:::{bucketName}/{container}/*");
                }

                var bucketPolicyModel = new BucketPolicyModel
                {
                    Version = "2012-10-17",
                    Statement =
                    [
                        new BucketPolicyStatementModel()
                        {
                            Sid = "PublicReadGetObject",
                            Effect = "Allow",
                            Principal = "*",
                            Action = "s3:GetObject",
                            Resource = resources
                        }
                    ]
                };

                var policy = JsonConvert.SerializeObject(bucketPolicyModel);

                var putBucketPolicyRequest = new PutBucketPolicyRequest
                {
                    BucketName = bucketName,
                    Policy = policy
                };

                await amazonS3.PutBucketPolicyAsync(putBucketPolicyRequest).ConfigureAwait(false);
            }
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
