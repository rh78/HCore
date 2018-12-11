using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HCore.Storage.Client.Impl
{
    public class GoogleCloudStorageClientImpl : IStorageClient
    {
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

        public async Task UploadFromStreamAsync(string containerName, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, Stream stream)
        {
            var credential = GoogleCredential.FromJson(_credentialsJson);

            using (var storageClient = await StorageClient.CreateAsync(credential).ConfigureAwait(false))
            {
                try
                {
                    await storageClient.CreateBucketAsync(_projectId, containerName).ConfigureAwait(false);
                }
                catch (Google.GoogleApiException e)
                when (e.Error.Code == 409)
                {
                    // bucket already exists, that's fine
                }
                
                // check if object exists
                try
                {
                    await storageClient.GetObjectAsync(containerName, fileName).ConfigureAwait(false);

                    // exists

                    await storageClient.DeleteObjectAsync(containerName, fileName).ConfigureAwait(false);
                }
                catch (Google.GoogleApiException e)
                when (e.Error.Code == 404)
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
                
                await storageClient.UploadObjectAsync(blockBlob, stream).ConfigureAwait(false);                
            }
        }
    }
}
