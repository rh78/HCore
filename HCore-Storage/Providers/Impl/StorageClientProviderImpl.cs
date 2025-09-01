using System;
using HCore.Storage.Client;
using HCore.Storage.Client.Impl;
using Microsoft.Extensions.Configuration;

namespace HCore.Storage.Providers.Impl
{
    internal class StorageClientProviderImpl : IStorageClientProvider
    {
        private readonly IStorageClient _storageClient;

        public StorageClientProviderImpl(IConfiguration configuration)
        {
            string implementation = configuration["Storage:Implementation"];

            if (string.IsNullOrEmpty(implementation))
                throw new Exception("Storage implementation specification is empty");

            var useGoogleCloud = implementation.Equals(StorageConstants.StorageImplementationGoogleCloud);
            var useAzure = implementation.Equals(StorageConstants.StorageImplementationAzure);
            var useAws = implementation.Equals(StorageConstants.StorageImplementationAws);

            if (!useGoogleCloud && !useAzure && !useAws)
                throw new Exception("Storage implementation specification is invalid");

            var connectionString = configuration["Storage:Account"];

            if (string.IsNullOrEmpty(connectionString))
                throw new Exception("Storage connection string is empty");

            if (useGoogleCloud)
            {
                _storageClient = new GoogleCloudStorageClientImpl(connectionString);
            }
            else if (useAzure)
            {
                _storageClient = new AzureStorageClientImpl(connectionString);
            }
            else
            {
                _storageClient = new AwsStorageClientImpl(connectionString);
            }
        }

        public IStorageClient GetStorageClient()
        {
            return _storageClient;
        }
    }
}
