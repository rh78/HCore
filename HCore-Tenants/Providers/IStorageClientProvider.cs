using HCore.Storage.Client;

namespace HCore.Tenants.Providers
{
    public interface IStorageClientProvider : Storage.Providers.IStorageClientProvider
    {
        IStorageClient GetStorageClient(long developerUuid, long tenantUuid);
    }
}
