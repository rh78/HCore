using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using HCore.Storage.Exceptions;
using HCore.Storage.Models;
using HCore.Web.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;

namespace HCore.Storage.Client.Impl
{
    public class AzureStorageClientImpl : IStorageClient
    {
        private readonly BlobServiceClient _blobServiceClient;

        public AzureStorageClientImpl(string connectionString)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task DownloadToStreamAsync(string containerName, string fileName, Stream stream)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

                var blobClient = containerClient.GetBlobClient(fileName);

                await blobClient.DownloadToAsync(stream).ConfigureAwait(false);
            }
            catch (RequestFailedException requestFailedException)
            {
                var statusCode = requestFailedException.Status;

                if (statusCode == (int)HttpStatusCode.NotFound)
                {
                    throw new ExternalServiceApiException(ExternalServiceApiException.CloudStorageFileNotFound, "The file was not found");
                }
                else if (statusCode == (int)HttpStatusCode.Forbidden ||
                    statusCode == (int)HttpStatusCode.Unauthorized)
                {
                    throw new ExternalServiceApiException(ExternalServiceApiException.CloudStorageFileAccessDenied, "Access to the file was denied");
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task<long> GetFileSizeAsync(string containerName, string fileName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

                var blobClient = containerClient.GetBlobClient(fileName);

                var blobProperties = await blobClient.GetPropertiesAsync().ConfigureAwait(false);

                return blobProperties.Value.ContentLength;
            }
            catch (RequestFailedException requestFailedException)
            {
                var statusCode = requestFailedException.Status;

                if (statusCode == (int)HttpStatusCode.NotFound)
                {
                    throw new ExternalServiceApiException(ExternalServiceApiException.CloudStorageFileNotFound, "The file was not found");
                }
                else if (statusCode == (int)HttpStatusCode.Forbidden ||
                    statusCode == (int)HttpStatusCode.Unauthorized)
                {
                    throw new ExternalServiceApiException(ExternalServiceApiException.CloudStorageFileAccessDenied, "Access to the file was denied");
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task<string> UploadFromStreamAsync(string containerName, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, Stream stream, bool overwriteIfExists, IProgress<long> progressHandler = null, string downloadFileName = null)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            await containerClient.CreateIfNotExistsAsync().ConfigureAwait(false);

            var blobClient = containerClient.GetBlobClient(fileName);

            if (overwriteIfExists)
            {
                await blobClient.DeleteIfExistsAsync().ConfigureAwait(false);
            }
            else
            {
                bool alreadyExists = await blobClient.ExistsAsync().ConfigureAwait(false);

                if (alreadyExists)
                {
                    if (!overwriteIfExists)
                    {
                        throw new AlreadyExistsException();
                    }
                }
            }

            var blobHttpHeaders = new BlobHttpHeaders();

            blobHttpHeaders.ContentType = mimeType;

            if (!string.IsNullOrEmpty(downloadFileName))
            {
                var contentDispositionHeader = new ContentDisposition() { FileName = downloadFileName };

                blobHttpHeaders.ContentDisposition = contentDispositionHeader.ToString();
            }

            var metadata = new Dictionary<string, string>();

            if (additionalHeaders != null) {
                foreach (var key in additionalHeaders.Keys)
                {
                    metadata[key] = additionalHeaders[key];
                }
            }

            Progress<long> innerProgressHandler = null;

            if (progressHandler != null)
            {
                innerProgressHandler = new Progress<long>();

                innerProgressHandler.ProgressChanged += (sender, bytesTransferred) =>
                {
                    progressHandler.Report(bytesTransferred);
                };
            }

            var blobUploadOptions = new BlobUploadOptions()
            {
                ProgressHandler = innerProgressHandler,
                HttpHeaders = blobHttpHeaders,
                Metadata = metadata
            };

            if (!overwriteIfExists)
            {
                blobUploadOptions.Conditions = new BlobRequestConditions
                {
                    IfNoneMatch = new ETag("*")
                };
            }

            var blobContentInfo = await blobClient.UploadAsync(stream, blobUploadOptions).ConfigureAwait(false);

            return blobClient.Uri.AbsoluteUri;
        }

        public async Task<string> UploadFromStreamLowLatencyProfileAsync(string containerName, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, Stream stream, bool containerIsPublic, IProgress<long> progressHandler = null, string downloadFileName = null)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            await containerClient.CreateIfNotExistsAsync().ConfigureAwait(false);

            if (containerIsPublic)
            {
                var containerProperties = await containerClient.GetPropertiesAsync().ConfigureAwait(false);

                if (containerProperties.Value.PublicAccess != PublicAccessType.Blob)
                {
                    await containerClient.SetAccessPolicyAsync(accessType: PublicAccessType.Blob).ConfigureAwait(false);
                }
            }

            var blobClient = containerClient.GetBlobClient(fileName);

            var blobHttpHeaders = new BlobHttpHeaders();

            blobHttpHeaders.ContentType = mimeType;

            if (!string.IsNullOrEmpty(downloadFileName))
            {
                var contentDispositionHeader = new ContentDisposition() { FileName = downloadFileName };

                blobHttpHeaders.ContentDisposition = contentDispositionHeader.ToString();
            }

            var metadata = new Dictionary<string, string>();

            if (additionalHeaders != null)
            {
                foreach (var key in additionalHeaders.Keys)
                {
                    metadata[key] = additionalHeaders[key];
                }
            }

            Progress<long> innerProgressHandler = null;

            if (progressHandler != null)
            {
                innerProgressHandler = new Progress<long>();

                innerProgressHandler.ProgressChanged += (sender, bytesTransferred) =>
                {
                    progressHandler.Report(bytesTransferred);
                };
            }

            var blobUploadOptions = new BlobUploadOptions()
            {
                ProgressHandler = innerProgressHandler,
                HttpHeaders = blobHttpHeaders,
                Metadata = metadata,
                Conditions = new BlobRequestConditions
                {
                    IfNoneMatch = new ETag("*")
                }
            };

            await blobClient.UploadAsync(stream, blobUploadOptions).ConfigureAwait(false);

            return blobClient.Uri.AbsoluteUri;
        }

        public async Task CreateContainerAsync(string containerName, bool isPublic)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            await containerClient.CreateIfNotExistsAsync().ConfigureAwait(false);

            if (isPublic)
            {
                var containerProperties = await containerClient.GetPropertiesAsync().ConfigureAwait(false);

                if (containerProperties.Value.PublicAccess != PublicAccessType.Blob)
                {
                    await containerClient.SetAccessPolicyAsync(accessType: PublicAccessType.Blob).ConfigureAwait(false);
                }
            }
        }

        public async Task DeleteContainerAsync(string containerName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            await containerClient.DeleteIfExistsAsync().ConfigureAwait(false);
        }

        public async Task DeleteFileAsync(string containerName, string fileName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

                await containerClient.DeleteBlobAsync(fileName).ConfigureAwait(false);
            }
            catch (RequestFailedException requestFailedException)
            {
                var statusCode = requestFailedException.Status;

                if (statusCode == (int)HttpStatusCode.NotFound)
                {
                    return;
                }
                else if (statusCode == (int)HttpStatusCode.Forbidden || statusCode == (int)HttpStatusCode.Unauthorized)
                {
                    throw new ExternalServiceApiException(ExternalServiceApiException.CloudStorageFileAccessDenied, "Access to the file was denied");
                }
                else
                {
                    throw;
                }
            }
        }

#pragma warning disable CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.
        public async Task<string> GetSignedDownloadUrlAsync(string containerName, string fileName, TimeSpan validityTimeSpan, string downloadFileName = null)
#pragma warning restore CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            var blobClient = containerClient.GetBlobClient(fileName);

            BlobSasBuilder blobSasBuilder = new BlobSasBuilder(BlobContainerSasPermissions.Read, DateTimeOffset.Now.Add(validityTimeSpan));

            if (!string.IsNullOrEmpty(downloadFileName))
            {
                var contentDispositionHeader = new ContentDisposition() { FileName = downloadFileName };

                blobSasBuilder.ContentDisposition = contentDispositionHeader.ToString();
            }

            var uri = blobClient.GenerateSasUri(blobSasBuilder);

            return uri.AbsoluteUri;
        }

        public async Task<ICollection<string>> GetStorageFileNamesAsync(string containerName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            var result = new List<string>();

            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {
                result.Add(blobItem.Name);
            }

            return result;
        }

        public async Task<long> GetStorageFileSizeAsync(string containerName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            long storageFileSize = 0;

            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {
                storageFileSize += (long)blobItem.Properties.ContentLength;
            }

            return storageFileSize;
        }

        public async IAsyncEnumerable<StorageItemModel> GetStorageItemsAsync(string containerName, int? pageSize = null)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            if (!await containerClient.ExistsAsync().ConfigureAwait(false))
            {
                yield break;
            }

            await foreach (var blobItemPage in containerClient.GetBlobsAsync().AsPages(pageSizeHint: pageSize))
            {
                foreach (var blobItem in blobItemPage.Values)
                {
                    yield return new StorageItemModel
                    {
                        Name = blobItem.Name,
                        CreatedOn = blobItem.Properties?.CreatedOn,
                        LastModified = blobItem.Properties?.LastModified,
                        LastAccessedOn = blobItem.Properties?.LastAccessedOn,
                        ContentLength = blobItem.Properties?.ContentLength,
                        ContentType = blobItem.Properties?.ContentType
                    };
                }
            }
        }
    }
}
