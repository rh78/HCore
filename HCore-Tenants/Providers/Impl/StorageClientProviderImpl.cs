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
                bool useGoogleCloud = implementation.Equals(StorageConstants.StorageImplementationGoogleCloud);
                bool useAzure = implementation.Equals(StorageConstants.StorageImplementationAzure);

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

            return _storageClient;
        }

        public async Task<IStorageClient> GetStorageClientAsync(long developerUuid, long tenantUuid)
        {
            var tenantInfo = await _tenantDataProvider.GetTenantByUuidThrowAsync(developerUuid, tenantUuid).ConfigureAwait(false);

            string implementation = tenantInfo.StorageImplementation;
            string connectionString = tenantInfo.StorageConnectionString;

            bool useGoogleCloud = implementation.Equals(StorageConstants.StorageImplementationGoogleCloud);
            bool useAzure = implementation.Equals(StorageConstants.StorageImplementationAzure);

            if (useGoogleCloud)
            {
                return new GoogleCloudStorageClientImpl(connectionString);
            }
            else if (useAzure)
            {
                return new AzureStorageClientImpl(connectionString);
            }
            else
            {
                return new AwsStorageClientImpl(connectionString);
            }
        }
    }
}
