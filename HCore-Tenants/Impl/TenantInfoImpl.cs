namespace HCore.Tenants.Impl
{
    internal class TenantInfoImpl : ITenantInfo
    {
        public long DeveloperUuid { get; internal set; }
        public string DeveloperAuthority { get; internal set; }
        public string DeveloperAudience { get; internal set; }
        public byte[] DeveloperCertificate { get; internal set; }
        public string DeveloperCertificatePassword { get; internal set; }
        public string DeveloperAuthCookieDomain { get; internal set; }

        public long TenantUuid { get; internal set; }

        public string Name { get; internal set; }
        public string LogoUrl { get; internal set; }

        public int PrimaryColor { get; internal set; }
        public int SecondaryColor { get; internal set; }
        public int TextOnPrimaryColor { get; internal set; }
        public int TextOnSecondaryColor { get; internal set; }

        public string SupportEmail { get; internal set; }
        public string NoreplyEmail { get; internal set; }

        public string ProductName { get; internal set; }

        public string ApiUrl { get; set; }
        public string WebUrl { get; set; }
    }
}
