using HCore.Tenants.Models;

namespace HCore.Identity.Services
{
    public interface IAuthInfo
    {
        string UserUuid { get; }
        
        ITenantInfo TenantInfo { get; }

        long? DeveloperUuid { get; }
        long? TenantUuid { get; }

        bool IsDeveloperAdmin { get; }

        bool IsAnonymous { get; }
    }
}
