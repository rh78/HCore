using HCore.Tenants.Models;

namespace HCore.Tenants.Providers
{
    public interface ITenantInfoAccessor
    {
        ITenantInfo TenantInfo { get; }
    }
}
