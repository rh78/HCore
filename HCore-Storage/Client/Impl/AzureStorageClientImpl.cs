using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
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
            var container = _cloudBlobClient.GetContainerReference(containerName);

            var blockBlob = container.GetBlockBlobReference(fileName);

            await blockBlob.DownloadToStreamAsync(stream).ConfigureAwait(false);
        }

        public async Task UploadFromStreamAsync(string containerName, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, Stream stream, bool overwriteIfExists)
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
                    throw new Exception("The target storage object already exists");
            }

            blockBlob.Properties.ContentType = mimeType;

            if (additionalHeaders != null) {
                foreach (var key in additionalHeaders.Keys)
                {
                    blockBlob.Metadata[key] = additionalHeaders[key];
                }
            }

            await blockBlob.UploadFromStreamAsync(stream).ConfigureAwait(false);            
        }

#pragma warning disable CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.
        public async Task<string> GetSignedDownloadUrlAsync(string containerName, string fileName, TimeSpan validityTimeSpan)
#pragma warning restore CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.
        {
            var container = _cloudBlobClient.GetContainerReference(containerName);

            var blockBlob = container.GetBlockBlobReference(fileName);

            SharedAccessBlobPolicy sasBlobPolicy = new SharedAccessBlobPolicy();

            sasBlobPolicy.SharedAccessExpiryTime = DateTimeOffset.Now.Add(validityTimeSpan);

            sasBlobPolicy.Permissions = SharedAccessBlobPermissions.Read;

            string token = blockBlob.GetSharedAccessSignature(sasBlobPolicy);

            return $"{blockBlob.Uri}{token}";
        }
    }
}
