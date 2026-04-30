using HCore.Tenants.Models;
using System.Globalization;
using System.Threading.Tasks;

namespace HCore.Tenants.Cache
{
    public interface ITenantCache
    {
        Task<ITenantInfo> GetTenantInfoByUuidAsync(long developerUuid, long tenantUuid, CultureInfo cultureInfo, bool isDraft, bool isSiteAnalysis);
        Task CreateOrUpdateTenantInfoForUuidAsync(long developerUuid, long tenantUuid, CultureInfo cultureInfo, bool isDraft, bool isSiteAnalysis, ITenantInfo tenantInfo);

        Task<ITenantInfo> GetTenantInfoBySubdomainLookupAsync(long developerUuid, string subDomainLookup, CultureInfo cultureInfo, bool isDraft, bool isSiteAnalysis);
        Task CreateOrUpdateTenantInfoForSubdomainLookupAsync(long developerUuid, string subDomainLookup, CultureInfo cultureInfo, bool isDraft, bool isSiteAnalysis, ITenantInfo tenantInfo);

        Task InvalidateTenantInfosAsync(long developerUuid, long tenantUuid);
    }
}
