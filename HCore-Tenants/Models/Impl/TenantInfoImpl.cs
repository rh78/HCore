using System.Globalization;

namespace HCore.Tenants.Models.Impl
{
    internal class TenantInfoImpl : ITenantInfo
    {
        public long DeveloperUuid { get; internal set; }
        public string DeveloperAuthority { get; internal set; }
        public string DeveloperAudience { get; internal set; }
        public byte[] DeveloperCertificate { get; internal set; }
        public string DeveloperCertificatePassword { get; internal set; }
        public string DeveloperAuthCookieDomain { get; internal set; }
        public string DeveloperName { get; internal set; }

        public string DeveloperPrivacyPolicyUrl { get; internal set; }
        public int? DeveloperPrivacyPolicyVersion { get; internal set; }

        public bool RequiresTermsAndConditions { get; internal set; }

        public string DeveloperTermsAndConditionsUrl { get; internal set; }
        public int? DeveloperTermsAndConditionsVersion { get; internal set; }

        public long TenantUuid { get; internal set; }

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

        public string PrimaryColorHex { get; internal set; }
        public string SecondaryColorHex { get; internal set; }
        public string TextOnPrimaryColorHex { get; internal set; }
        public string TextOnSecondaryColorHex { get; internal set; }

        public string SupportEmail { get; internal set; }
        public string NoreplyEmail { get; internal set; }

        public string CustomInvitationEmailTextPrefix { get; internal set; }
        public string CustomInvitationEmailTextSuffix { get; internal set; }

        public string ProductName { get; internal set; }

        public string DefaultCulture { get; internal set; }

        public string BackendApiUrl { get; set; }
        public string FrontendApiUrl { get; set; }

        public string WebUrl { get; set; }

        public bool UsersAreExternallyManaged { get; set; }

        public string ExternalAuthenticationMethod { get; set; }

        public string OidcClientId { get; set; }
        public string OidcClientSecret { get; set; }

        public string OidcEndpointUrl { get; set; }

        public string ExternalDirectoryType { get; set; }
        public string ExternalDirectoryHost { get; set; }
        public int? ExternalDirectoryPort { get; set; }

        public bool? ExternalDirectoryUsesSsl { get; set; }

        public byte[] ExternalDirectorySslCertificate { get; set; }

        public string ExternalDirectoryAccountDistinguishedName { get; set; }

        public string ExternalDirectoryPassword { get; set; }

        public string ExternalDirectoryLoginAttribute { get; set; }

        public string ExternalDirectoryBaseContexts { get; set; }

        public string ExternalDirectoryUserFilter { get; set; }
        public string ExternalDirectoryGroupFilter { get; set; }

        public int? ExternalDirectorySyncIntervalSeconds { get; set; }

        public string ExternalDirectoryAdministratorGroupUuid { get; set; }
    }
}
