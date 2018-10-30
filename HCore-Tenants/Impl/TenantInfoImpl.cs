namespace HCore.Tenants.Impl
{
    internal class TenantInfoImpl : ITenantInfo
    {
        public long DeveloperUuid { get; internal set; }
        public string DeveloperAuthority { get; internal set; }
        public string DeveloperAudience { get; internal set; }
        public byte[] DeveloperCertificate { get; internal set; }
        public string CertificatePassword { get; internal set; }
        public string DeveloperAuthCookieDomain { get; internal set; }

        public long TenantUuid { get; internal set; }
        public string Name { get; internal set; }
        public string LogoUrl { get; internal set; }

        public string ApiUrl { get; set; }
        public string WebUrl { get; set; }
    }
}
