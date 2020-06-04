using HCore.Tenants.Models;
using System.Threading.Tasks;

namespace HCore.Tenants.Cache
{
    public interface ITenantCache
    {
        Task<ITenantInfo> GetTenantInfoByUuidAsync(long developerUuid, long tenantUuid);
        Task CreateOrUpdateTenantInfoForUuidAsync(long developerUuid, long tenantUuid, ITenantInfo tenantInfo);

        Task<ITenantInfo> GetTenantInfoBySubdomainLookupAsync(long developerUuid, string subDomainLookup);
        Task CreateOrUpdateTenantInfoForSubdomainLookupAsync(long developerUuid, string subDomainLookup, ITenantInfo tenantInfo);

        Task InvalidateTenantInfosAsync(long developerUuid);
    }
}
