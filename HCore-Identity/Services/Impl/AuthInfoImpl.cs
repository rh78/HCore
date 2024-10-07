using HCore.Tenants.Models;

namespace HCore.Identity.Services.Impl
{
    public class AuthInfoImpl : IAuthInfo
    {
        public string UserUuid { get; set; }

        public ITenantInfo TenantInfo { get; set; }

        public long? DeveloperUuid { get => TenantInfo?.DeveloperUuid; }

        public long? TenantUuid { get => TenantInfo?.TenantUuid;  }

        public bool IsDeveloperAdmin { get; set; }

        public bool IsOemAdmin { get; set; }

        public bool IsAnonymous { get; set; }
    }
}
