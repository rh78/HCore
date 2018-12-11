using System;
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

            bool useGoogleCloud = implementation.Equals(StorageConstants.StorageImplementationGoogleCloud);

            if(_storageClient == null)
            {
                if (useGoogleCloud)
                {
                    _storageClient = new GoogleCloudStorageClientImpl(connectionString);
                }
                else
                {
                    _storageClient = new AzureStorageClientImpl(connectionString);
                }
            }

            return _storageClient;
        }

        public IStorageClient GetStorageClient(long developerUuid, long tenantUuid)
        {
            var tenantInfo = _tenantDataProvider.LookupTenantByUuid(developerUuid, tenantUuid);

            if (tenantInfo == null)
                throw new Exception($"No tenant found for developer UUID {developerUuid} and tenant UUID {tenantUuid}");

            string implementation = tenantInfo.StorageImplementation;
            string connectionString = tenantInfo.StorageConnectionString;

            bool useGoogleCloud = implementation.Equals(StorageConstants.StorageImplementationGoogleCloud);

            if (useGoogleCloud)
            {
                return new GoogleCloudStorageClientImpl(connectionString);
            }
            else
            {
                return new AzureStorageClientImpl(connectionString);
            }            
        }
    }
}
