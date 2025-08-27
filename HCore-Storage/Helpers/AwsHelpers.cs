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

        public static async Task CreateContainerAsync(IAmazonS3 amazonS3, string containerName, bool isPublic)
        {
            ArgumentNullException.ThrowIfNull(amazonS3);
            ArgumentNullException.ThrowIfNullOrEmpty(containerName);

            GetBucketLocationResponse getBucketLocationResponse = null;

            try
            {
                getBucketLocationResponse = await amazonS3.GetBucketLocationAsync(containerName).ConfigureAwait(false);
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.StatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }
            }

            if (getBucketLocationResponse == null)
            {
                var putBucketRequest = new PutBucketRequest
                {
                    BucketName = containerName
                };

                await amazonS3.PutBucketAsync(putBucketRequest).ConfigureAwait(false);

                // https://docs.aws.amazon.com/AmazonS3/latest/userguide/access-control-block-public-access.html

                var putPublicAccessBlockRequest = new PutPublicAccessBlockRequest
                {
                    BucketName = containerName,
                    PublicAccessBlockConfiguration = new PublicAccessBlockConfiguration
                    {
                        BlockPublicAcls = !isPublic,
                        IgnorePublicAcls = !isPublic,
                        BlockPublicPolicy = !isPublic,
                        RestrictPublicBuckets = !isPublic
                    }
                };

                await amazonS3.PutPublicAccessBlockAsync(putPublicAccessBlockRequest).ConfigureAwait(false);
            }

            if (isPublic)
            {
                var isBucketPublic = await IsBucketPublicAsync(amazonS3, containerName).ConfigureAwait(false);

                if (!isBucketPublic)
                {
                    // https://docs.aws.amazon.com/AmazonS3/latest/userguide/WebsiteAccessPermissionsReqd.html

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
                                Resource = $"arn:aws:s3:::{containerName}/*"
                            }
                        ]
                    };

                    var policy = JsonConvert.SerializeObject(bucketPolicyModel);

                    var putBucketPolicyRequest = new PutBucketPolicyRequest
                    {
                        BucketName = containerName,
                        Policy = policy
                    };

                    await amazonS3.PutBucketPolicyAsync(putBucketPolicyRequest).ConfigureAwait(false);
                }
            }
        }

        public static async Task<bool> IsBucketPublicAsync(IAmazonS3 amazonS3, string containerName)
        {
            ArgumentNullException.ThrowIfNull(amazonS3);
            ArgumentNullException.ThrowIfNullOrEmpty(containerName);

            var getBucketPolicyResponse = await amazonS3.GetBucketPolicyAsync(containerName).ConfigureAwait(false);

            if (string.IsNullOrEmpty(getBucketPolicyResponse.Policy))
            {
                return false;
            }

            var bucketPolicyModel = JsonConvert.DeserializeObject<BucketPolicyModel>(getBucketPolicyResponse.Policy);

            if (bucketPolicyModel == null ||
                bucketPolicyModel.Statement == null)
            {
                return false;
            }

            foreach (var bucketPolicyStatementModel in bucketPolicyModel.Statement)
            {
                if (!string.Equals(bucketPolicyStatementModel.Effect, "Allow"))
                {
                    continue;
                }

                var isPublicPrincipal = bucketPolicyStatementModel.Principal switch
                {
                    string principal => string.Equals(principal, "*"),
                    IDictionary<string, object> principalDict => principalDict.TryGetValue("AWS", out var aws) && string.Equals(aws?.ToString(), "*"),
                    _ => false
                };

                if (!isPublicPrincipal)
                {
                    continue;
                }

                var hasGetObject = bucketPolicyStatementModel.Action switch
                {
                    string action => string.Equals(action, "s3:GetObject"),
                    IEnumerable<object> actions => actions.Contains("s3:GetObject"),
                    _ => false
                };

                if (hasGetObject)
                {
                    return true;
                }
            }

            return false;
        }

        public static string GetAbsoluteUrl(IAmazonS3 amazonS3, string containerName, string fileName)
        {
            ArgumentNullException.ThrowIfNull(amazonS3);
            ArgumentNullException.ThrowIfNullOrEmpty(containerName);
            ArgumentNullException.ThrowIfNullOrEmpty(fileName);

            var endpoint = amazonS3.DetermineServiceOperationEndpoint(new GetObjectRequest
            {
                BucketName = containerName,
                Key = fileName
            });

            var uriBuilder = new UriBuilder(endpoint.URL)
            {
                Path = Uri.EscapeDataString(fileName)
            };

            return uriBuilder.ToString();
        }
    }
}
