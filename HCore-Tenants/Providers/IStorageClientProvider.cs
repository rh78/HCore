using HCore.Storage.Client;
using System.Threading.Tasks;

namespace HCore.Tenants.Providers
{
    public interface IStorageClientProvider : Storage.Providers.IStorageClientProvider
    {
        Task<IStorageClient> GetStorageClientAsync(long developerUuid, long tenantUuid);
    }
}
