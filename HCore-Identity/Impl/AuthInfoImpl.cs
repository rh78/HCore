using HCore.Tenants;

namespace HCore.Identity.Impl
{
    internal class AuthInfoImpl : IAuthInfo
    {
        public string UserUuid { get; internal set; }

        public ITenantInfo TenantInfo { get; internal set; }

        public long? DeveloperUuid { get => TenantInfo?.DeveloperUuid; }

        public long? TenantUuid { get => TenantInfo?.TenantUuid;  }

        public bool IsDeveloperAdmin { get; internal set; }
    }
}
