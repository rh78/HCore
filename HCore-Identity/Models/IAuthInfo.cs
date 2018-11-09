using HCore.Tenants.Models;

namespace HCore.Identity.Models
{
    public interface IAuthInfo
    {
        string UserUuid { get; }
        
        ITenantInfo TenantInfo { get; }

        long? DeveloperUuid { get; }
        long? TenantUuid { get; }

        bool IsDeveloperAdmin { get; }
    }
}
