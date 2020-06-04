using HCore.Tenants.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCore.Tenants.Providers
{
    public interface ITenantDataProvider
    {
        List<string> DeveloperWildcardSubdomains { get; }
        List<IDeveloperInfo> Developers { get; }

        IDeveloperInfo GetDeveloper(long developerUuid);

        Task<(string, ITenantInfo)> GetTenantByHostAsync(string host);
        Task<ITenantInfo> GetTenantByUuidThrowAsync(long developerUuid, long tenantUuid);
        
        int? HealthCheckPort { get; }
        string HealthCheckTenantHost { get; }
    }
}
