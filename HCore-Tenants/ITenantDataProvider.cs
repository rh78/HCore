using System.Collections.Generic;

namespace HCore.Tenants
{
    public interface ITenantDataProvider
    {
        List<ITenantInfo> Tenants { get; }
        List<IDeveloperInfo> Developers { get; }

        ITenantInfo LookupTenantByHost(string host);
        ITenantInfo LookupTenantByUuid(long developerUuid, long tenantUuid);       
    }
}
