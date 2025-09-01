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
using HCore.Web.Exceptions;

namespace HCore.Storage.Client.Impl
{
    public class AwsStorageClientImpl : IStorageClient, IDisposable
    {
        private const int _defaultPageSize = 1000;

        private readonly IAmazonS3 _amazonS3;
        private readonly string _bucketPrefix;

        private bool _disposed;

        public AwsStorageClientImpl(string connectionString)
        {
            var connectionInfoByKey = AwsHelpers.GetConnectionInfoByKey(connectionString);

            _amazonS3 = AwsHelpers.GetAmazonS3(connectionInfoByKey);
            _bucketPrefix = AwsHelpers.GetBucketPrefix(connectionInfoByKey);
        }

        public async Task DownloadToStreamAsync(string containerName, string fileName, Stream stream)
        {
            containerName = ApplyPrefix(containerName);

            using var getObjectResponse = await GetObjectResponseAsync(containerName, fileName, required: true).ConfigureAwait(false);

            await getObjectResponse.ResponseStream.CopyToAsync(stream).ConfigureAwait(false);
        }

        private string ApplyPrefix(string containerName)
        {
            if (!containerName.StartsWith(_bucketPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return $"{_bucketPrefix}{containerName}";
            }

            return containerName;
        }

        private async Task<GetObjectResponse> GetObjectResponseAsync(string containerName, string fileName, bool required)
        {
            try
            {
                return await _amazonS3.GetObjectAsync(containerName, fileName).ConfigureAwait(false);
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
            containerName = ApplyPrefix(containerName);

            var getObjectResponse = await GetObjectResponseAsync(containerName, fileName, required: true).ConfigureAwait(false);

            return getObjectResponse.ResponseStream;
        }

        public async Task<long> GetFileSizeAsync(string containerName, string fileName)
        {
            containerName = ApplyPrefix(containerName);

            using var getObjectResponse = await GetObjectResponseAsync(containerName, fileName, required: true).ConfigureAwait(false);

            return getObjectResponse.ContentLength;
        }

        public async Task<string> InitializeChunksAsync(string containerName, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, bool overwriteIfExists, string downloadFileName = null)
        {
            containerName = ApplyPrefix(containerName);

            await CreateContainerAsync(containerName, isPublic: false).ConfigureAwait(false);

            await HandleExistingObjectAsync(containerName, fileName, overwriteIfExists).ConfigureAwait(false);

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

            var initiateMultipartUploadResponse = await _amazonS3.InitiateMultipartUploadAsync(initiateMultipartUploadRequest).ConfigureAwait(false);

            return initiateMultipartUploadResponse.UploadId;
        }

        private async Task HandleExistingObjectAsync(string containerName, string fileName, bool overwriteIfExists)
        {
            if (overwriteIfExists)
            {
                await DeleteObjectAsync(containerName, fileName).ConfigureAwait(false);
            }
            else
            {
                using var getObjectResponse = await GetObjectResponseAsync(containerName, fileName, required: false).ConfigureAwait(false);

                if (getObjectResponse != null && !overwriteIfExists)
                {
                    throw new AlreadyExistsException();
                }
            }
        }

        public async Task<string> UploadChunkFromStreamAsync(string containerName, string externalId, string fileName, long chunkId, long blockStart, Stream stream, bool overwriteIfExists, IProgress<long> progressHandler = null)
        {
            containerName = ApplyPrefix(containerName);

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

            var uploadPartResponse = await _amazonS3.UploadPartAsync(uploadPartRequest).ConfigureAwait(false);

            return uploadPartResponse.ETag;
        }

        public async Task<string> FinalizeChunksAsync(string containerName, string externalId, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, List<long> chunkIds, List<string> eTags, bool overwriteIfExists, string downloadFileName = null)
        {
            if (chunkIds.Count != eTags.Count)
            {
                throw new ArgumentException("chunkIds and eTags must have the same count");
            }

            containerName = ApplyPrefix(containerName);

            await CreateContainerAsync(containerName, isPublic: false).ConfigureAwait(false);

            await HandleExistingObjectAsync(containerName, fileName, overwriteIfExists).ConfigureAwait(false);

            var partETags = new List<PartETag>();

            for (int i = 0; i < chunkIds.Count; i++)
            {
                var chunkId = (int)chunkIds[i];
                var eTag = eTags[i];

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

            var completeMultipartUploadResponse = await _amazonS3.CompleteMultipartUploadAsync(completeMultipartUploadRequest).ConfigureAwait(false);

            var absoluteUrl = AwsHelpers.GetAbsoluteUrl(_amazonS3, containerName, fileName);

            return absoluteUrl;
        }

        public async Task<string> UploadFromStreamAsync(string containerName, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, Stream stream, bool overwriteIfExists, IProgress<long> progressHandler = null, string downloadFileName = null)
        {
            containerName = ApplyPrefix(containerName);

            await CreateContainerAsync(containerName, isPublic: false).ConfigureAwait(false);

            await HandleExistingObjectAsync(containerName, fileName, overwriteIfExists).ConfigureAwait(false);

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

            using var transferUtility = new TransferUtility(_amazonS3);

            await transferUtility.UploadAsync(transferUtilityUploadRequest);

            var absoluteUrl = AwsHelpers.GetAbsoluteUrl(_amazonS3, containerName, fileName);

            return absoluteUrl;
        }

        public async Task<string> UploadFromStreamLowLatencyProfileAsync(string containerName, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, Stream stream, bool containerIsPublic, IProgress<long> progressHandler = null, string downloadFileName = null)
        {
            containerName = ApplyPrefix(containerName);

            await CreateContainerAsync(containerName, isPublic: containerIsPublic).ConfigureAwait(false);

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

            using var transferUtility = new TransferUtility(_amazonS3);

            await transferUtility.UploadAsync(transferUtilityUploadRequest);

            var absoluteUrl = AwsHelpers.GetAbsoluteUrl(_amazonS3, containerName, fileName);

            return absoluteUrl;
        }

        public Task CreateContainerAsync(string containerName, bool isPublic)
        {
            containerName = ApplyPrefix(containerName);

            return AwsHelpers.CreateContainerAsync(_amazonS3, containerName, isPublic);
        }

        public async Task DeleteContainerAsync(string containerName)
        {
            containerName = ApplyPrefix(containerName);

            string continuationToken = null;

            do
            {
                var listObjectsV2Response = await ListObjectsV2Async(containerName, continuationToken).ConfigureAwait(false);

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

                await _amazonS3.DeleteObjectsAsync(deleteObjectsRequest).ConfigureAwait(false);

                continuationToken = listObjectsV2Response.NextContinuationToken;

            } while (!string.IsNullOrEmpty(continuationToken));

            try
            {
                await _amazonS3.DeleteBucketAsync(containerName).ConfigureAwait(false);
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.StatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }
            }
        }

        private async Task<ListObjectsV2Response> ListObjectsV2Async(string containerName, string continuationToken, int? maxKeys = null)
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
                listObjectsV2Response = await _amazonS3.ListObjectsV2Async(listObjectsV2Request).ConfigureAwait(false);
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

        public Task DeleteFileAsync(string containerName, string fileName)
        {
            containerName = ApplyPrefix(containerName);

            return DeleteObjectAsync(containerName, fileName);
        }

        private async Task DeleteObjectAsync(string containerName, string fileName)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(containerName);
            ArgumentNullException.ThrowIfNullOrEmpty(fileName);

            try
            {
                await _amazonS3.DeleteObjectAsync(containerName, fileName).ConfigureAwait(false);
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
            containerName = ApplyPrefix(containerName);

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

            var url = await _amazonS3.GetPreSignedURLAsync(getPreSignedUrlRequest).ConfigureAwait(false);

            return url;
        }

        public async Task<ICollection<string>> GetStorageFileNamesAsync(string containerName)
        {
            containerName = ApplyPrefix(containerName);

            var names = new List<string>();

            await foreach (var storageItem in GetStorageItemsAsync(containerName).ConfigureAwait(false))
            {
                names.Add(storageItem.Name);
            }

            return names;
        }

        public async Task<long> GetStorageFileSizeAsync(string containerName)
        {
            containerName = ApplyPrefix(containerName);

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
            containerName = ApplyPrefix(containerName);

            string continuationToken = null;

            do
            {
                var listObjectsV2Response = await ListObjectsV2Async(containerName, continuationToken, maxKeys: pageSize).ConfigureAwait(false);

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

        public void Dispose()
        {
            Dispose(disposing: true);

            GC.SuppressFinalize(this);

            if (!_disposed)
            {
                _amazonS3.Dispose();

                _disposed = true;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (!disposing)
            {
                return;
            }

            _amazonS3.Dispose();

            _disposed = true;
        }
    }
}
