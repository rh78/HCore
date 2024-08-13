using HCore.Tenants.Database.SqlServer.Models.Impl;
using System.Security.Cryptography.X509Certificates;

namespace HCore.Tenants.Models
{
    public interface IDeveloperInfo
    {
        long DeveloperUuid { get; }
        string Authority { get; }
        string Audience { get; }
        X509Certificate2 Certificate { get; }
        string AuthCookieDomain { get; }
        string HostPattern { get; }

        string DefaultEcbBackendApiUrlSuffix { get; }
        string DefaultPortalsBackendApiUrlSuffix { get; }

        string DefaultFrontendApiUrlSuffix { get; }
        string DefaultWebUrlSuffix { get; }

        string Name { get; }

        string LogoSvgUrl { get; }
        string LogoPngUrl { get; }
        string IconIcoUrl { get; }
        string AppleTouchIconUrl { get; }

        string CustomCss { get; }

        string StorageImplementation { get; }
        string StorageConnectionString { get; }
        
        int PrimaryColor { get; }
        int SecondaryColor { get; }
        int TextOnPrimaryColor { get; }
        int TextOnSecondaryColor { get; }

        string SupportEmail { get; }
        string SupportEmailDisplayName { get; }

        string NoreplyEmail { get; }
        string NoreplyEmailDisplayName { get; }

        string PrivacyPolicyUrl { get; }
        int? PrivacyPolicyVersion { get; }

        EmailSettingsModel EmailSettings { get; }

        string EcbProductName { get; }
        string PortalsProductName { get; }
    }
}
