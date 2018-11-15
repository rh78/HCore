namespace HCore.Tenants.Models.Impl
{
    internal class DeveloperInfoImpl : IDeveloperInfo
    {
        public long DeveloperUuid { get; internal set; }
        public string Authority { get; internal set; }
        public string Audience { get; internal set; }
        public byte[] Certificate { get; internal set; }
        public string CertificatePassword { get; internal set; }
        public string AuthCookieDomain { get; internal set; }

        public string Name { get; internal set; }

        public string LogoSvgUrl { get; internal set; }
        public string LogoPngUrl { get; internal set; }

        public int PrimaryColor { get; internal set; }
        public int SecondaryColor { get; internal set; }
        public int TextOnPrimaryColor { get; internal set; }
        public int TextOnSecondaryColor { get; internal set; }

        public string SupportEmail { get; internal set; }
        public string NoreplyEmail { get; internal set; }

        public string ProductName { get; internal set; }
    }
}
