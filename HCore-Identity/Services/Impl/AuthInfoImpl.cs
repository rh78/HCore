using HCore.Tenants.Models;

namespace HCore.Identity.Services.Impl
{
    public class AuthInfoImpl : IAuthInfo
    {
        public string UserUuid { get; internal set; }

        public ITenantInfo TenantInfo { get; internal set; }

        public long? DeveloperUuid { get => TenantInfo?.DeveloperUuid; }

        public long? TenantUuid { get => TenantInfo?.TenantUuid;  }

        public bool IsDeveloperAdmin { get; internal set; }

        public bool IsAnonymous { get; internal set; }
    }
}
