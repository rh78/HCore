using HCore.Tenants.Database.SqlServer.Models.Impl;
using System.Security.Cryptography.X509Certificates;

namespace HCore.Tenants.Models.Impl
{
    internal class DeveloperInfoImpl : IDeveloperInfo
    {
        public long DeveloperUuid { get; internal set; }
        public string Authority { get; internal set; }
        public string Audience { get; internal set; }
        public X509Certificate2 Certificate { get; internal set; }
        public string AuthCookieDomain { get; internal set; }

        public string DefaultEcbBackendApiUrlSuffix { get; internal set; }
        public string DefaultPortalsBackendApiUrlSuffix { get; internal set; }

        public string DefaultFrontendApiUrlSuffix { get; internal set; }
        public string DefaultWebUrlSuffix { get; internal set; }

        public string Name { get; internal set; }

        public string LogoSvgUrl { get; internal set; }
        public string LogoPngUrl { get; internal set; }
        public string IconIcoUrl { get; internal set; }

        public string StorageImplementation { get; set; }
        public string StorageConnectionString { get; set; }

        public int PrimaryColor { get; internal set; }
        public int SecondaryColor { get; internal set; }
        public int TextOnPrimaryColor { get; internal set; }
        public int TextOnSecondaryColor { get; internal set; }

        public string SupportEmail { get; internal set; }
        public string SupportEmailDisplayName { get; internal set; }

        public string NoreplyEmail { get; internal set; }
        public string NoreplyEmailDisplayName { get; internal set; }

        public EmailSettingsModel EmailSettings { get; set; }

        public string ProductName { get; internal set; }
    }
}
