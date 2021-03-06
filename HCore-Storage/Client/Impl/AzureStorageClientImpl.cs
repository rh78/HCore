﻿using HCore.Storage.Exceptions;
using HCore.Web.Exceptions;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Core.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace HCore.Storage.Client.Impl
{
    public class AzureStorageClientImpl : IStorageClient
    {
        private readonly CloudBlobClient _cloudBlobClient;

        public AzureStorageClientImpl(string connectionString)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            _cloudBlobClient = storageAccount.CreateCloudBlobClient();            
        }

        public async Task DownloadToStreamAsync(string containerName, string fileName, Stream stream)
        {
            try
            {
                var container = _cloudBlobClient.GetContainerReference(containerName);

                var blockBlob = container.GetBlockBlobReference(fileName);

                await blockBlob.DownloadToStreamAsync(stream).ConfigureAwait(false);
            }
            catch (StorageException storageException)
            {
                var requestInformation = storageException.RequestInformation;

                if (requestInformation == null)
                    throw;

                var httpStatusCode = requestInformation.HttpStatusCode;

                if (httpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    throw new ExternalServiceApiException(ExternalServiceApiException.CloudStorageFileNotFound, "The file was not found");
                }
                else if (httpStatusCode == (int)HttpStatusCode.Forbidden ||
                    httpStatusCode == (int)HttpStatusCode.Unauthorized)
                {
                    throw new ExternalServiceApiException(ExternalServiceApiException.CloudStorageFileAccessDenied, "Access to the file was denied");
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task<string> UploadFromStreamAsync(string containerName, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, Stream stream, bool overwriteIfExists, IProgress<long> progressHandler = null)
        {
            var container = _cloudBlobClient.GetContainerReference(containerName);

            await container.CreateIfNotExistsAsync().ConfigureAwait(false);

            var blockBlob = container.GetBlockBlobReference(fileName);

            if (overwriteIfExists)
            {
                await blockBlob.DeleteIfExistsAsync().ConfigureAwait(false);
            }
            else
            {
                bool alreadyExists = await blockBlob.ExistsAsync().ConfigureAwait(false);

                if (alreadyExists)
                {
                    if (!overwriteIfExists)
                    {
                        throw new AlreadyExistsException();
                    }
                }
            }

            blockBlob.Properties.ContentType = mimeType;

            if (additionalHeaders != null) {
                foreach (var key in additionalHeaders.Keys)
                {
                    blockBlob.Metadata[key] = additionalHeaders[key];
                }
            }

            Progress<StorageProgress> progress = null;

            if (progressHandler != null)
            {
                progress = new Progress<StorageProgress>();

                progress.ProgressChanged += (s, e) =>
                {
                    progressHandler.Report(e.BytesTransferred);
                };
            }

            await blockBlob.UploadFromStreamAsync(stream, accessCondition: null, options: null, operationContext: null, progress, default(CancellationToken)).ConfigureAwait(false);

            return blockBlob.Uri.AbsoluteUri;
        }

        public async Task<string> UploadFromStreamLowLatencyProfileAsync(string containerName, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, Stream stream, bool containerIsPublic, IProgress<long> progressHandler = null)
        {
            var container = _cloudBlobClient.GetContainerReference(containerName);
            
            await container.CreateIfNotExistsAsync().ConfigureAwait(false);

            if (containerIsPublic && container.Properties.PublicAccess != BlobContainerPublicAccessType.Blob)
            {
                await container.SetPermissionsAsync(new BlobContainerPermissions()
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                }).ConfigureAwait(false);
            }

            var blockBlob = container.GetBlockBlobReference(fileName);

            blockBlob.Properties.ContentType = mimeType;

            if (additionalHeaders != null)
            {
                foreach (var key in additionalHeaders.Keys)
                {
                    blockBlob.Metadata[key] = additionalHeaders[key];
                }
            }

            Progress<StorageProgress> progress = null;

            if (progressHandler != null)
            {
                progress = new Progress<StorageProgress>();

                progress.ProgressChanged += (s, e) =>
                {
                    progressHandler.Report(e.BytesTransferred);
                };
            }

            await blockBlob.UploadFromStreamAsync(stream, accessCondition: null, options: null, operationContext: null, progress, default(CancellationToken)).ConfigureAwait(false);

            return blockBlob.Uri.AbsoluteUri;
        }

#pragma warning disable CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.
        public async Task<string> GetSignedDownloadUrlAsync(string containerName, string fileName, TimeSpan validityTimeSpan, string downloadFileName = null)
#pragma warning restore CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.
        {
            var container = _cloudBlobClient.GetContainerReference(containerName);

            var blockBlob = container.GetBlockBlobReference(fileName);

            SharedAccessBlobPolicy sasBlobPolicy = new SharedAccessBlobPolicy();

            sasBlobPolicy.SharedAccessExpiryTime = DateTimeOffset.Now.Add(validityTimeSpan);

            sasBlobPolicy.Permissions = SharedAccessBlobPermissions.Read;

            string token;

            if (!string.IsNullOrEmpty(downloadFileName))
            {
                var sasBlobHeaders = new SharedAccessBlobHeaders()
                {
                    ContentDisposition = $"attachment; filename=\"{downloadFileName}\""
                };

                token = blockBlob.GetSharedAccessSignature(sasBlobPolicy, sasBlobHeaders);
            }
            else
            {
                token = blockBlob.GetSharedAccessSignature(sasBlobPolicy);
            }

            return $"{blockBlob.Uri}{token}";
        }
    }
}
