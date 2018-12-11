using HCore.Storage.Client;
using HCore.Storage.Client.Impl;
using Microsoft.Extensions.Configuration;
using System;

namespace HCore.Storage.Providers.Impl
{
    internal class StorageClientProviderImpl : IStorageClientProvider
    {
        private bool _useGoogleCloud;

        private IStorageClient _storageClient;

        private string _connectionString;

        public StorageClientProviderImpl(IConfiguration configuration)
        {
            string implementation = configuration["Storage:Implementation"];

            if (string.IsNullOrEmpty(implementation))
                throw new Exception("Storage implementation specification is empty");

            if (!implementation.Equals(StorageConstants.StorageImplementationAzure) && !implementation.Equals(StorageConstants.StorageImplementationGoogleCloud))
                throw new Exception("Storage implementation specification is invalid");

            _useGoogleCloud = implementation.Equals(StorageConstants.StorageImplementationGoogleCloud);

            _connectionString = configuration["Storage:Account"];

            if (string.IsNullOrEmpty(_connectionString))
                throw new Exception("Storage connection string is empty");

            if (_useGoogleCloud)
            {
                _storageClient = new GoogleCloudStorageClientImpl(_connectionString);
            }
            else
            {
                _storageClient = new AzureStorageClientImpl(_connectionString);
            }
        }

        public IStorageClient GetStorageClient()
        {
            return _storageClient;            
        }
    }
}
