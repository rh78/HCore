using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using HCore.Storage.Exceptions;
using HCore.Storage.Helpers;
using HCore.Storage.Models;
using HCore.Storage.Models.AwsStorage;
using HCore.Storage.Streams;
using HCore.Web.Exceptions;
using Newtonsoft.Json;

namespace HCore.Storage.Client.Impl
{
    public class AwsStorageClientImpl : IStorageClient
    {
        private const int _defaultPageSize = 1000;

        private readonly IDictionary<string, string> _connectionInfoByKey;

        public AwsStorageClientImpl(string connectionString)
        {
            _connectionInfoByKey = AwsHelpers.GetConnectionInfoByKey(connectionString);
        }

        public async Task DownloadToStreamAsync(string containerName, string fileName, Stream stream)
        {
            using var amazonS3 = AwsHelpers.GetAmazonS3(_connectionInfoByKey);

            using var getObjectResponse = await GetObjectResponseAsync(amazonS3, containerName, fileName, required: true).ConfigureAwait(false);

            await getObjectResponse.ResponseStream.CopyToAsync(stream).ConfigureAwait(false);
        }

        private static async Task<GetObjectResponse> GetObjectResponseAsync(IAmazonS3 amazonS3, string containerName, string fileName, bool required)
        {
            try
            {
                return await amazonS3.GetObjectAsync(containerName, fileName).ConfigureAwait(false);
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.StatusCode == HttpStatusCode.NotFound)
                {
                    if (required)
                    {
                        throw new ExternalServiceApiException(ExternalServiceApiException.CloudStorageFileNotFound, "The file was not found");
                    }

                    return null;
                }

                if (amazonS3Exception.StatusCode == HttpStatusCode.Forbidden || amazonS3Exception.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new ExternalServiceApiException(ExternalServiceApiException.CloudStorageFileAccessDenied, "Access to the file was denied");
                }

                throw;
            }
        }

        public async Task<Stream> OpenReadAsync(string containerName, string fileName)
        {
            using var amazonS3 = AwsHelpers.GetAmazonS3(_connectionInfoByKey);

            var getObjectResponse = await GetObjectResponseAsync(amazonS3, containerName, fileName, required: true).ConfigureAwait(false);

            return new S3SeekableStream(getObjectResponse.ResponseStream, getObjectResponse.ContentLength);
        }

        public async Task<long> GetFileSizeAsync(string containerName, string fileName)
        {
            using var amazonS3 = AwsHelpers.GetAmazonS3(_connectionInfoByKey);

            using var getObjectResponse = await GetObjectResponseAsync(amazonS3, containerName, fileName, required: true).ConfigureAwait(false);

            return getObjectResponse.ContentLength;
        }

        public async Task<string> InitializeChunksAsync(string containerName, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, bool overwriteIfExists, string downloadFileName = null)
        {
            using var amazonS3 = AwsHelpers.GetAmazonS3(_connectionInfoByKey);

            await CreateContainerAsync(amazonS3, containerName, isPublic: false).ConfigureAwait(false);

            await HandleExistingObjectAsync(amazonS3, containerName, fileName, overwriteIfExists).ConfigureAwait(false);

            var initiateMultipartUploadRequest = new InitiateMultipartUploadRequest
            {
                BucketName = containerName,
                Key = fileName,
                ContentType = mimeType
            };

            if (!string.IsNullOrEmpty(downloadFileName))
            {
                var contentDispositionHeaderValue = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = downloadFileName
                };

                initiateMultipartUploadRequest.Headers.ContentDisposition = contentDispositionHeaderValue.ToString();
            }

            if (additionalHeaders != null)
            {
                foreach (var key in additionalHeaders.Keys)
                {
                    initiateMultipartUploadRequest.Metadata[key] = additionalHeaders[key];
                }
            }

            var initiateMultipartUploadResponse = await amazonS3.InitiateMultipartUploadAsync(initiateMultipartUploadRequest).ConfigureAwait(false);

            return initiateMultipartUploadResponse.UploadId;
        }

        private static async Task HandleExistingObjectAsync(IAmazonS3 amazonS3, string bucketName, string key, bool overwriteIfExists)
        {
            if (overwriteIfExists)
            {
                await DeleteObjectAsync(amazonS3, bucketName, key).ConfigureAwait(false);
            }
            else
            {
                using var getObjectResponse = await GetObjectResponseAsync(amazonS3, bucketName, key, required: false).ConfigureAwait(false);

                if (getObjectResponse != null && !overwriteIfExists)
                {
                    throw new AlreadyExistsException();
                }
            }
        }

        public async Task<string> UploadChunkFromStreamAsync(string containerName, string externalId, string fileName, long chunkId, long blockStart, Stream stream, bool overwriteIfExists, IProgress<long> progressHandler = null)
        {
            using var amazonS3 = AwsHelpers.GetAmazonS3(_connectionInfoByKey);

            var uploadPartRequest = new UploadPartRequest()
            {
                BucketName = containerName,
                Key = fileName,
                UploadId = externalId,
                PartNumber = (int)chunkId,
                InputStream = stream
            };

            if (progressHandler != null)
            {
                uploadPartRequest.StreamTransferProgress += (sender, args) =>
                {
                    progressHandler.Report(args.TransferredBytes);
                };
            }

            var uploadPartResponse = await amazonS3.UploadPartAsync(uploadPartRequest).ConfigureAwait(false);

            return uploadPartResponse.ETag;
        }

        public async Task<string> FinalizeChunksAsync(string containerName, string externalId, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, List<long> chunkIds, List<string> eTags, bool overwriteIfExists, string downloadFileName = null)
        {
            using var amazonS3 = AwsHelpers.GetAmazonS3(_connectionInfoByKey);

            await CreateContainerAsync(amazonS3, containerName, isPublic: false).ConfigureAwait(false);

            await HandleExistingObjectAsync(amazonS3, containerName, fileName, overwriteIfExists).ConfigureAwait(false);

            var partETags = new List<PartETag>();

            for (int i = 0; i < chunkIds.Count; i++)
            {
                var chunkId = (int)chunkIds.ElementAt(i);
                var eTag = eTags.ElementAt(i);

                var partETag = new PartETag(chunkId, eTag);

                partETags.Add(partETag);
            }

            var completeMultipartUploadRequest = new CompleteMultipartUploadRequest
            {
                BucketName = containerName,
                Key = fileName,
                UploadId = externalId,
                PartETags = partETags
            };

            if (!overwriteIfExists)
            {
                completeMultipartUploadRequest.IfNoneMatch = "*";
            }

            var completeMultipartUploadResponse = await amazonS3.CompleteMultipartUploadAsync(completeMultipartUploadRequest).ConfigureAwait(false);

            var absoluteUrl = GetAbsoluteUrl(amazonS3, containerName, fileName);

            return absoluteUrl;
        }

        private static string GetAbsoluteUrl(IAmazonS3 amazonS3, string containerName, string fileName)
        {
            var uriBuilder = new UriBuilder(Uri.UriSchemeHttps, hostName: $"{containerName}.s3.{amazonS3.Config.RegionEndpoint.SystemName}.amazonaws.com")
            {
                Path = Uri.EscapeDataString(fileName)
            };

            return uriBuilder.ToString();
        }

        public async Task<string> UploadFromStreamAsync(string containerName, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, Stream stream, bool overwriteIfExists, IProgress<long> progressHandler = null, string downloadFileName = null)
        {
            using var amazonS3 = AwsHelpers.GetAmazonS3(_connectionInfoByKey);

            await CreateContainerAsync(amazonS3, containerName, isPublic: false).ConfigureAwait(false);

            await HandleExistingObjectAsync(amazonS3, containerName, fileName, overwriteIfExists).ConfigureAwait(false);

            var transferUtilityUploadRequest = new TransferUtilityUploadRequest
            {
                BucketName = containerName,
                Key = fileName,
                InputStream = stream,
                ContentType = mimeType
            };

            if (!string.IsNullOrEmpty(downloadFileName))
            {
                var contentDispositionHeaderValue = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = downloadFileName
                };

                transferUtilityUploadRequest.Headers.ContentDisposition = contentDispositionHeaderValue.ToString();
            }

            if (additionalHeaders != null)
            {
                foreach (var key in additionalHeaders.Keys)
                {
                    transferUtilityUploadRequest.Metadata[key] = additionalHeaders[key];
                }
            }

            if (progressHandler != null)
            {
                transferUtilityUploadRequest.UploadProgressEvent += (sender, args) =>
                {
                    progressHandler.Report(args.TransferredBytes);
                };
            }

            if (!overwriteIfExists)
            {
                transferUtilityUploadRequest.IfNoneMatch = "*";
            }

            using var transferUtility = new TransferUtility(amazonS3);

            await transferUtility.UploadAsync(transferUtilityUploadRequest);

            var absoluteUrl = GetAbsoluteUrl(amazonS3, containerName, fileName);

            return absoluteUrl;
        }

        public async Task<string> UploadFromStreamLowLatencyProfileAsync(string containerName, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, Stream stream, bool containerIsPublic, IProgress<long> progressHandler = null, string downloadFileName = null)
        {
            using var amazonS3 = AwsHelpers.GetAmazonS3(_connectionInfoByKey);

            await CreateContainerAsync(amazonS3, containerName, isPublic: containerIsPublic).ConfigureAwait(false);

            var transferUtilityUploadRequest = new TransferUtilityUploadRequest()
            {
                BucketName = containerName,
                Key = fileName,
                InputStream = stream,
                ContentType = mimeType
            };

            if (!string.IsNullOrEmpty(downloadFileName))
            {
                var contentDispositionHeaderValue = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = downloadFileName
                };

                transferUtilityUploadRequest.Headers.ContentDisposition = contentDispositionHeaderValue.ToString();
            }

            if (additionalHeaders != null)
            {
                foreach (var key in additionalHeaders.Keys)
                {
                    transferUtilityUploadRequest.Metadata[key] = additionalHeaders[key];
                }
            }

            if (progressHandler != null)
            {
                transferUtilityUploadRequest.UploadProgressEvent += (sender, args) =>
                {
                    progressHandler.Report(args.TransferredBytes);
                };
            }

            using var transferUtility = new TransferUtility(amazonS3);

            await transferUtility.UploadAsync(transferUtilityUploadRequest);

            var absoluteUrl = GetAbsoluteUrl(amazonS3, containerName, fileName);

            return absoluteUrl;
        }

        public async Task CreateContainerAsync(string containerName, bool isPublic)
        {
            using var amazonS3 = AwsHelpers.GetAmazonS3(_connectionInfoByKey);

            await CreateContainerAsync(amazonS3, containerName, isPublic);
        }

        private static async Task CreateContainerAsync(IAmazonS3 amazonS3, string containerName, bool isPublic)
        {
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
                        BlockPublicAcls = false,
                        IgnorePublicAcls = false,
                        BlockPublicPolicy = false,
                        RestrictPublicBuckets = false
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

        private static async Task<bool> IsBucketPublicAsync(IAmazonS3 amazonS3, string containerName)
        {
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

                var isPublicPrincipal = false;

                if (bucketPolicyStatementModel.Principal is string principal)
                {
                    isPublicPrincipal = string.Equals(principal, "*");

                }
                else if (bucketPolicyStatementModel.Principal is IDictionary<string, object> principalDict)
                {
                    isPublicPrincipal = principalDict.TryGetValue("AWS", out var aws) &&
                        aws != null &&
                        string.Equals(aws.ToString(), "*");
                }

                if (!isPublicPrincipal)
                {
                    continue;
                }

                var hasGetObject = false;

                if (bucketPolicyStatementModel.Action is string action)
                {
                    hasGetObject = string.Equals(action, "s3:GetObject");
                }
                else if (bucketPolicyStatementModel.Action is IEnumerable<object> actions)
                {
                    hasGetObject = actions.Contains("s3:GetObject");
                }

                if (hasGetObject)
                {
                    return true;
                }
            }

            return false;
        }

        public async Task DeleteContainerAsync(string containerName)
        {
            using var amazonS3 = AwsHelpers.GetAmazonS3(_connectionInfoByKey);

            string continuationToken = null;

            do
            {
                var listObjectsV2Response = await ListObjectsV2Async(amazonS3, containerName, continuationToken).ConfigureAwait(false);

                if (listObjectsV2Response == null || !listObjectsV2Response.S3Objects.Any())
                {
                    break;
                }

                var deleteObjectsRequest = new DeleteObjectsRequest
                {
                    BucketName = containerName,
                    Objects = listObjectsV2Response.S3Objects
                        .Select(s3Object => new KeyVersion()
                        {
                            Key = s3Object.Key
                        })
                        .ToList(),
                    Quiet = true,
                };

                await amazonS3.DeleteObjectsAsync(deleteObjectsRequest).ConfigureAwait(false);

                continuationToken = listObjectsV2Response.NextContinuationToken;

            } while (!string.IsNullOrEmpty(continuationToken));

            try
            {
                await amazonS3.DeleteBucketAsync(containerName).ConfigureAwait(false);
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.StatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }
            }
        }

        private static async Task<ListObjectsV2Response> ListObjectsV2Async(IAmazonS3 amazonS3, string containerName, string continuationToken, int? maxKeys = null)
        {
            var listObjectsV2Request = new ListObjectsV2Request
            {
                BucketName = containerName,
                ContinuationToken = continuationToken,
                MaxKeys = maxKeys ?? _defaultPageSize,
            };

            ListObjectsV2Response listObjectsV2Response = null;

            try
            {
                listObjectsV2Response = await amazonS3.ListObjectsV2Async(listObjectsV2Request).ConfigureAwait(false);
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.StatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }
            }

            return listObjectsV2Response;
        }

        public async Task DeleteFileAsync(string containerName, string fileName)
        {
            using var amazonS3 = AwsHelpers.GetAmazonS3(_connectionInfoByKey);

            await DeleteObjectAsync(amazonS3, containerName, fileName);
        }

        private static async Task DeleteObjectAsync(IAmazonS3 amazonS3, string containerName, string fileName)
        {
            ArgumentNullException.ThrowIfNull(amazonS3);
            ArgumentNullException.ThrowIfNullOrEmpty(containerName);
            ArgumentNullException.ThrowIfNullOrEmpty(fileName);

            try
            {
                await amazonS3.DeleteObjectAsync(containerName, fileName).ConfigureAwait(false);
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.StatusCode == HttpStatusCode.NotFound)
                {
                    return;
                }

                if (amazonS3Exception.StatusCode == HttpStatusCode.Forbidden || amazonS3Exception.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new ExternalServiceApiException(ExternalServiceApiException.CloudStorageFileAccessDenied, "Access to the file was denied");
                }

                throw;
            }
        }

        public async Task<string> GetSignedDownloadUrlAsync(string containerName, string fileName, TimeSpan validityTimeSpan, string downloadFileName = null)
        {
            var getPreSignedUrlRequest = new GetPreSignedUrlRequest
            {
                BucketName = containerName,
                Key = fileName,
                Expires = DateTime.UtcNow.Add(validityTimeSpan),
                Verb = HttpVerb.GET
            };

            if (!string.IsNullOrEmpty(downloadFileName))
            {
                var contentDispositionHeaderValue = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = downloadFileName
                };

                getPreSignedUrlRequest.ResponseHeaderOverrides.ContentDisposition = contentDispositionHeaderValue.ToString();
            }

            using var amazonS3 = AwsHelpers.GetAmazonS3(_connectionInfoByKey);

            var url = await amazonS3.GetPreSignedURLAsync(getPreSignedUrlRequest).ConfigureAwait(false);

            return url;
        }

        public async Task<ICollection<string>> GetStorageFileNamesAsync(string containerName)
        {
            var names = new List<string>();

            await foreach (var storageItem in GetStorageItemsAsync(containerName).ConfigureAwait(false))
            {
                names.Add(storageItem.Name);
            }

            return names;
        }

        public async Task<long> GetStorageFileSizeAsync(string containerName)
        {
            long storageFileSize = 0;

            await foreach (var storageItem in GetStorageItemsAsync(containerName).ConfigureAwait(false))
            {
                if (!storageItem.ContentLength.HasValue)
                {
                    continue;
                }

                storageFileSize += storageItem.ContentLength.Value;
            }

            return storageFileSize;
        }

        public async IAsyncEnumerable<StorageItemModel> GetStorageItemsAsync(string containerName, int? pageSize = null)
        {
            using var amazonS3 = AwsHelpers.GetAmazonS3(_connectionInfoByKey);

            string continuationToken = null;

            do
            {
                var listObjectsV2Response = await ListObjectsV2Async(amazonS3, containerName, continuationToken, maxKeys: pageSize).ConfigureAwait(false);

                if (listObjectsV2Response == null || !listObjectsV2Response.S3Objects.Any())
                {
                    break;
                }

                foreach (var s3Object in listObjectsV2Response.S3Objects)
                {
                    yield return new StorageItemModel
                    {
                        Name = s3Object.Key,
                        LastModified = s3Object.LastModified,
                        ContentLength = s3Object.Size
                    };
                }

                continuationToken = listObjectsV2Response.NextContinuationToken;

            } while (!string.IsNullOrEmpty(continuationToken));
        }
    }
}
