using System;
using System.Threading.Tasks;
using HCore.Storage;
using HCore.Storage.Client;
using HCore.Storage.Client.Impl;

namespace HCore.Tenants.Providers.Impl
{
    internal class StorageClientProviderImpl : IStorageClientProvider
    {
        private readonly ITenantDataProvider _tenantDataProvider;
        private readonly ITenantInfoAccessor _tenantInfoAccessor;

        private IStorageClient _storageClient;

        public StorageClientProviderImpl(ITenantDataProvider tenantDataProvider, ITenantInfoAccessor tenantInfoAccessor)
        {
            _tenantDataProvider = tenantDataProvider;
            _tenantInfoAccessor = tenantInfoAccessor;
        }

        public IStorageClient GetStorageClient()
        {
            var tenantInfo = _tenantInfoAccessor.TenantInfo;

            string implementation = tenantInfo.StorageImplementation;
            string connectionString = tenantInfo.StorageConnectionString;

            if (_storageClient == null)
            {
                _storageClient = implementation switch
                {
                    StorageConstants.StorageImplementationGoogleCloud => new GoogleCloudStorageClientImpl(connectionString),
                    StorageConstants.StorageImplementationAzure => new AzureStorageClientImpl(connectionString),
                    StorageConstants.StorageImplementationAws => new AwsStorageClientImpl(connectionString),
                    _ => throw new Exception("Storage implementation specification is invalid")
                };
            }

            return _storageClient;
        }

        public async Task<IStorageClient> GetStorageClientAsync(long developerUuid, long tenantUuid)
        {
            var tenantInfo = await _tenantDataProvider.GetTenantByUuidThrowAsync(developerUuid, tenantUuid).ConfigureAwait(false);

            string implementation = tenantInfo.StorageImplementation;
            string connectionString = tenantInfo.StorageConnectionString;

            IStorageClient storageClient = implementation switch
            {
                StorageConstants.StorageImplementationGoogleCloud => new GoogleCloudStorageClientImpl(connectionString),
                StorageConstants.StorageImplementationAzure => new AzureStorageClientImpl(connectionString),
                StorageConstants.StorageImplementationAws => new AwsStorageClientImpl(connectionString),
                _ => throw new Exception("Storage implementation specification is invalid")
            };

            return storageClient;
        }
    }
}
