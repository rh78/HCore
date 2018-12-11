using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
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

        public async Task UploadFromStreamAsync(string containerName, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, Stream stream)
        {
            var container = _cloudBlobClient.GetContainerReference(containerName);

            await container.CreateIfNotExistsAsync().ConfigureAwait(false);

            var blockBlob = container.GetBlockBlobReference(fileName);

            await blockBlob.DeleteIfExistsAsync().ConfigureAwait(false);

            blockBlob.Properties.ContentType = mimeType;

            if (additionalHeaders != null) {
                foreach (var key in additionalHeaders.Keys)
                {
                    blockBlob.Metadata[key] = additionalHeaders[key];
                }
            }

            await blockBlob.UploadFromStreamAsync(stream).ConfigureAwait(false);            
        }
    }
}
