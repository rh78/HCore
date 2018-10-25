namespace HCore.Tenants.Impl
{
    internal class TenantInfoImpl : ITenantInfo
    {
        public long DeveloperUuid { get; internal set; }
        public string DeveloperAuthority { get; internal set; }
        public byte[] DeveloperCertificate { get; internal set; }

        public long TenantUuid { get; internal set; }
        public string Name { get; internal set; }
        public string LogoUrl { get; internal set; }
    }
}
