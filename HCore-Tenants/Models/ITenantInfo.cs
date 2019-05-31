using System;
using System.Security.Cryptography.X509Certificates;

namespace HCore.Tenants.Models
{
    public interface ITenantInfo
    {
        long DeveloperUuid { get; }
        string DeveloperAuthority { get; }
        string DeveloperAudience { get; }
        X509Certificate2 DeveloperCertificate { get; }
        string DeveloperAuthCookieDomain { get; }
        string DeveloperName { get; }

        string DeveloperPrivacyPolicyUrl { get; }
        int? DeveloperPrivacyPolicyVersion { get; }

        bool RequiresTermsAndConditions { get; }

        string DeveloperTermsAndConditionsUrl { get; }
        int? DeveloperTermsAndConditionsVersion { get; }

        long TenantUuid { get; }

        string Name { get; }

        string LogoSvgUrl { get; }
        string LogoPngUrl { get; }
        string IconIcoUrl { get; }

        string StorageImplementation { get; }
        string StorageConnectionString { get; }
        
        int PrimaryColor { get; }
        int SecondaryColor { get; }
        int TextOnPrimaryColor { get; }
        int TextOnSecondaryColor { get; }

        string PrimaryColorHex { get; }
        string SecondaryColorHex { get; }
        string TextOnPrimaryColorHex { get; }
        string TextOnSecondaryColorHex { get; }

        string SupportEmail { get; }
        string NoreplyEmail { get; }

        string CustomInvitationEmailTextPrefix { get; }
        string CustomInvitationEmailTextSuffix { get; }

        string ProductName { get; }

        string DefaultCulture { get; }

        string BackendApiUrl { get; }
        string FrontendApiUrl { get; }

        string WebUrl { get; }

        bool UsersAreExternallyManaged { get; }

        string ExternalAuthenticationMethod { get; }

        string OidcClientId { get; }
        string OidcClientSecret { get; }

        string OidcEndpointUrl { get; }

        string SamlEntityId { get; }
        string SamlMetadataLocation { get; }

        string SamlProviderUrl { get; }

        X509Certificate2 SamlCertificate { get; }

        string ExternalDirectoryType { get; }
        string ExternalDirectoryHost { get; }
        int? ExternalDirectoryPort { get; }

        bool? ExternalDirectoryUsesSsl { get; }

        X509Certificate2 ExternalDirectorySslCertificate { get; }

        string ExternalDirectoryAccountDistinguishedName { get; }

        string ExternalDirectoryPassword { get; }

        string ExternalDirectoryLoginAttribute { get; }

        string ExternalDirectoryBaseContexts { get; }

        string ExternalDirectoryUserFilter { get; }
        string ExternalDirectoryGroupFilter { get; }

        int? ExternalDirectorySyncIntervalSeconds { get; }

        string ExternalDirectoryAdministratorGroupUuid { get; }
    }
}
