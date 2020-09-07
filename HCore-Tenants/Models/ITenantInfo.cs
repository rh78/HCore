using HCore.Tenants.Database.SqlServer.Models.Impl;
using System;
using System.Security.Cryptography.X509Certificates;

namespace HCore.Tenants.Models
{
    public interface ITenantInfo
    {
        long DeveloperUuid { get; }
        string DeveloperAuthority { get; }
        string DeveloperAudience { get; }
        string DeveloperAuthCookieDomain { get; }
        string DeveloperName { get; }

        string DeveloperPrivacyPolicyUrl { get; }
        int? DeveloperPrivacyPolicyVersion { get; }

        bool RequiresTermsAndConditions { get; }

        string DeveloperTermsAndConditionsUrl { get; }
        int? DeveloperTermsAndConditionsVersion { get; }

        long TenantUuid { get; }

        string Name { get; }

        string SubdomainPattern { get; }

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
        string SupportEmailDisplayName { get; }

        string NoreplyEmail { get; }
        string NoreplyEmailDisplayName { get; }

        EmailSettingsModel EmailSettings { get; set; }

        string ProductName { get; }

        string DefaultCulture { get; }
        string DefaultCurrency { get; }

        string EcbBackendApiUrl { get; }
        string PortalsBackendApiUrl { get; }

        string FrontendApiUrl { get; }

        string WebUrl { get; }

        bool UsersAreExternallyManaged { get; }
        bool ExternalUsersAreManuallyManaged { get; }

        string ExternalAuthenticationMethod { get; }

        string OidcClientId { get; }
        string OidcClientSecret { get; }

        string OidcEndpointUrl { get; }

        string SamlEntityId { get; }

        string SamlPeerEntityId { get; set; }

        string SamlPeerIdpMetadataLocation { get; set;  }
        string SamlPeerIdpMetadata { get; set; }

        bool SamlAllowWeakSigningAlgorithm { get; }

        string ExternalDirectoryType { get; }
        string ExternalDirectoryHost { get; }
        int? ExternalDirectoryPort { get; }

        bool? ExternalDirectoryUsesSsl { get; }

        string ExternalDirectoryAccountDistinguishedName { get; }

        string ExternalDirectoryPassword { get; }

        string ExternalDirectoryLoginAttribute { get; }

        string ExternalDirectoryBaseContexts { get; }

        string ExternalDirectoryUserFilter { get; }
        string ExternalDirectoryGroupFilter { get; }

        int? ExternalDirectorySyncIntervalSeconds { get; }

        string ExternalDirectoryAdministratorGroupUuid { get; }

        string CustomTenantSettingsJson { get; }

        TCustomTenantSettingsDataType GetCustomTenantSettings<TCustomTenantSettingsDataType>();

        string AdditionalCacheKey { get; set;  }

        bool RequiresDevAdminSsoReplacement { get; }
        
        string DevAdminSsoReplacementSamlPeerEntityId { get; }
        string DevAdminSsoReplacementSamlPeerIdpMetadataLocation { get; }

        public string CreatedByUserUuid { get; }

        public long? MapDeveloperUuid { get; }
        public long? MapTenantUuid { get; }

        public long? MapCustomUuid { get; }

        public int Version { get; }

        public DateTimeOffset CreatedAt { get; }
        public DateTimeOffset? LastUpdatedAt { get; }

        ITenantInfo Clone();

        public X509Certificate2 GetDeveloperCertificate();
        public X509Certificate2 GetSamlCertificate();
        public X509Certificate2 GetExternalDirectorySslCertificate();
    }
}
