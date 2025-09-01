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

            var connectionString = configuration["Storage:Account"];

            if (string.IsNullOrEmpty(connectionString))
                throw new Exception("Storage connection string is empty");

            IStorageClient storageClient = implementation switch
            {
                StorageConstants.StorageImplementationGoogleCloud => new GoogleCloudStorageClientImpl(connectionString),
                StorageConstants.StorageImplementationAzure => new AzureStorageClientImpl(connectionString),
                StorageConstants.StorageImplementationAws => new AwsStorageClientImpl(connectionString),
                _ => throw new Exception("Storage implementation specification is invalid")
            };
        }

        public IStorageClient GetStorageClient()
        {
            return _storageClient;
        }
    }
}
