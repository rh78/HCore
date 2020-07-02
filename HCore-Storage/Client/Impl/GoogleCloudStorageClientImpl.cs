using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Storage.v1.Data;
using Google.Cloud.Storage.V1;
using HCore.Web.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HCore.Storage.Client.Impl
{
    public class GoogleCloudStorageClientImpl : IStorageClient
    {
        private const int ChunkSize = 5 * 1024 * 1024; // 5 MB

        private readonly string _projectId;
        private readonly string _credentialsJson;

        public GoogleCloudStorageClientImpl(string connectionString)
        {
            int firstIndex = connectionString.IndexOf(":");
            if (firstIndex == -1)
                throw new Exception("Google Cloud connection string is invalid");

            _projectId = connectionString.Substring(0, firstIndex);

            if (string.IsNullOrEmpty(_projectId))
                throw new Exception("The Google Cloud project ID is invalid");

            _credentialsJson = connectionString.Substring(firstIndex + 1);

            if (string.IsNullOrEmpty(_credentialsJson))
                throw new Exception("The Google Cloud credentials JSON is invalid");
        }

        public async Task DownloadToStreamAsync(string containerName, string fileName, Stream stream)
        {
            var credential = GoogleCredential.FromJson(_credentialsJson);

            try
            {
                using (var storageClient = await StorageClient.CreateAsync(credential).ConfigureAwait(false))
                {
                    var blockBlob = await storageClient.GetObjectAsync(containerName, fileName).ConfigureAwait(false);

                    await storageClient.DownloadObjectAsync(blockBlob, stream, new DownloadObjectOptions()
                    {
                        ChunkSize = ChunkSize
                    }).ConfigureAwait(false);
                }
            } catch (GoogleApiException e)
            {
                if (e.HttpStatusCode == HttpStatusCode.NotFound)
                {
                    throw new ExternalServiceApiException(ExternalServiceApiException.CloudStorageFileNotFound, "The file was not found");
                }
                else if (e.HttpStatusCode == HttpStatusCode.Forbidden ||
                    e.HttpStatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new ExternalServiceApiException(ExternalServiceApiException.CloudStorageFileAccessDenied, "Access to the file was denied");
                }
                else
                {
                    throw e;
                }
            }
        }

        public async Task<string> UploadFromStreamAsync(string containerName, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, Stream stream, bool overwriteIfExists)
        {
            var credential = GoogleCredential.FromJson(_credentialsJson);

            using (var storageClient = await StorageClient.CreateAsync(credential).ConfigureAwait(false))
            {
                try
                {
                    await storageClient.GetBucketAsync(containerName).ConfigureAwait(false);
                }
                catch (GoogleApiException e)
                when (e.HttpStatusCode == HttpStatusCode.NotFound)
                {
                    // we need to create the bucket

                    try
                    {
                        var bucket = new Bucket()
                        {
                            Name = containerName,
                            Location = "eu"
                        };

                        await storageClient.CreateBucketAsync(_projectId, bucket).ConfigureAwait(false);
                    }
                    catch (GoogleApiException)
                    when (e.HttpStatusCode == HttpStatusCode.Conflict)
                    {
                        // bucket already exists, that's fine
                    }
                }                
                
                // check if object exists
                try
                {
                    await storageClient.GetObjectAsync(containerName, fileName).ConfigureAwait(false);

                    // exists

                    if (!overwriteIfExists)
                        throw new Exception("The target storage object already exists");

                    await storageClient.DeleteObjectAsync(containerName, fileName).ConfigureAwait(false);
                }
                catch (GoogleApiException e)
                when (e.HttpStatusCode == HttpStatusCode.NotFound)
                {
                    // not found, that's fine
                }

                var blockBlob = new Google.Apis.Storage.v1.Data.Object
                {
                    Bucket = containerName,
                    Name = fileName,
                    ContentType = mimeType
                };

                if (additionalHeaders != null)
                {
                    blockBlob.Metadata = additionalHeaders;
                }
                
                var result = await storageClient.UploadObjectAsync(blockBlob, stream, new UploadObjectOptions()
                {
                    ChunkSize = ChunkSize
                }).ConfigureAwait(false);

                return result.MediaLink;
            }
        }

        public async Task<string> UploadFromStreamLowLatencyProfileAsync(string containerName, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, Stream stream, bool containerIsPublic)
        {
            var credential = GoogleCredential.FromJson(_credentialsJson);

            using (var storageClient = await StorageClient.CreateAsync(credential).ConfigureAwait(false))
            {
                try
                {
                    await storageClient.GetBucketAsync(containerName).ConfigureAwait(false);
                }
                catch (GoogleApiException e)
                when (e.HttpStatusCode == HttpStatusCode.NotFound)
                {
                    if (containerIsPublic)
                    {
                        // not yet supported for Google Cloud storage

                        throw new NotImplementedException("Container public level is not yet implemented");
                    }

                    // we need to create the bucket

                    try
                    {
                        var bucket = new Bucket()
                        {
                            Name = containerName,
                            Location = "eu"
                        };

                        await storageClient.CreateBucketAsync(_projectId, bucket).ConfigureAwait(false);
                    }
                    catch (GoogleApiException)
                    when (e.HttpStatusCode == HttpStatusCode.Conflict)
                    {
                        // bucket already exists, that's fine
                    }
                }

                var blockBlob = new Google.Apis.Storage.v1.Data.Object
                {
                    Bucket = containerName,
                    Name = fileName,
                    ContentType = mimeType
                };

                if (additionalHeaders != null)
                {
                    blockBlob.Metadata = additionalHeaders;
                }

                var result = await storageClient.UploadObjectAsync(blockBlob, stream, new UploadObjectOptions()
                {
                    ChunkSize = ChunkSize
                }).ConfigureAwait(false);

                return result.MediaLink;
            }
        }

        public async Task<string> GetSignedDownloadUrlAsync(string containerName, string fileName, TimeSpan validityTimeSpan, string? downloadFileName = null)
        {
            var credential = GoogleCredential.FromJson(_credentialsJson)
                .CreateScoped(new string[] { "https://www.googleapis.com/auth/devstorage.read_only" })
                .UnderlyingCredential as ServiceAccountCredential;

            var urlSigner = UrlSigner.FromServiceAccountCredential(credential);

            string signedUrl;

            if (!string.IsNullOrEmpty(downloadFileName))
            {
                signedUrl = await urlSigner.SignAsync(containerName, fileName, validityTimeSpan, HttpMethod.Get, contentHeaders: new Dictionary<string, IEnumerable<string>>()
                {
                    { 
                        "Content-Disposition", new [] { $"attachment; filename=\"{downloadFileName}\"" } 
                    }
                }).ConfigureAwait(false);
            }
            else
            {
                signedUrl = await urlSigner.SignAsync(containerName, fileName, validityTimeSpan, HttpMethod.Get).ConfigureAwait(false);
            }

            return signedUrl;
        }
    }
}
