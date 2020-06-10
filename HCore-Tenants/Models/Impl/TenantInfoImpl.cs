using Newtonsoft.Json;
using System;
using System.Security.Cryptography.X509Certificates;

namespace HCore.Tenants.Models.Impl
{
    [Serializable]
    internal class TenantInfoImpl : ITenantInfo
    {
        public long DeveloperUuid { get; internal set; }
        public string DeveloperAuthority { get; internal set; }
        public string DeveloperAudience { get; internal set; }
        public X509Certificate2 DeveloperCertificate { get; internal set; }
        public string DeveloperAuthCookieDomain { get; internal set; }
        public string DeveloperName { get; internal set; }

        public string DeveloperPrivacyPolicyUrl { get; internal set; }
        public int? DeveloperPrivacyPolicyVersion { get; internal set; }

        public bool RequiresTermsAndConditions { get; internal set; }

        public string DeveloperTermsAndConditionsUrl { get; internal set; }
        public int? DeveloperTermsAndConditionsVersion { get; internal set; }

        public long TenantUuid { get; internal set; }

        public string Name { get; internal set; }

        public string SubdomainPattern { get; internal set; }

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
        public string DefaultCurrency { get; internal set; }

        public string BackendApiUrl { get; set; }
        public string FrontendApiUrl { get; set; }

        public string WebUrl { get; set; }

        public bool UsersAreExternallyManaged { get; set; }
        public bool ExternalUsersAreManuallyManaged { get; set; }

        public string ExternalAuthenticationMethod { get; set; }

        public string OidcClientId { get; set; }
        public string OidcClientSecret { get; set; }

        public string OidcEndpointUrl { get; set; }

        public string SamlEntityId { get; set; }

        public string SamlPeerEntityId { get; set; }

        public string SamlPeerIdpMetadataLocation { get; set; }
        public string SamlPeerIdpMetadata { get; set; }

        public X509Certificate2 SamlCertificate { get; set; }

        public bool SamlAllowWeakSigningAlgorithm { get; set; }

        public string ExternalDirectoryType { get; set; }
        public string ExternalDirectoryHost { get; set; }
        public int? ExternalDirectoryPort { get; set; }

        public bool? ExternalDirectoryUsesSsl { get; set; }

        public X509Certificate2 ExternalDirectorySslCertificate { get; set; }

        public string ExternalDirectoryAccountDistinguishedName { get; set; }

        public string ExternalDirectoryPassword { get; set; }

        public string ExternalDirectoryLoginAttribute { get; set; }

        public string ExternalDirectoryBaseContexts { get; set; }

        public string ExternalDirectoryUserFilter { get; set; }
        public string ExternalDirectoryGroupFilter { get; set; }

        public int? ExternalDirectorySyncIntervalSeconds { get; set; }

        public string ExternalDirectoryAdministratorGroupUuid { get; set; }

        public string CustomTenantSettingsJson { get; set; }

        public string AdditionalCacheKey { get; set; }

        public bool RequiresDevAdminSsoReplacement { get; set; }

        public string DevAdminSsoReplacementSamlPeerEntityId { get; set; }
        public string DevAdminSsoReplacementSamlPeerIdpMetadataLocation { get; set; }

        public string CreatedByUserUuid { get; set; }

        public int Version { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastUpdatedAt { get; set; }

        public TCustomTenantSettingsDataType GetCustomTenantSettings<TCustomTenantSettingsDataType>()
        {
            if (CustomTenantSettingsJson == null)
                return default;

            return JsonConvert.DeserializeObject<TCustomTenantSettingsDataType>(CustomTenantSettingsJson);
        }

        public ITenantInfo Clone()
        {
            return new TenantInfoImpl()
            {
                DeveloperUuid = DeveloperUuid,
                DeveloperAuthority = DeveloperAuthority,
                DeveloperAudience = DeveloperAudience,
                DeveloperCertificate = DeveloperCertificate,
                DeveloperAuthCookieDomain = DeveloperAuthCookieDomain,
                DeveloperName = DeveloperName,
                DeveloperPrivacyPolicyUrl = DeveloperPrivacyPolicyUrl,
                DeveloperPrivacyPolicyVersion = DeveloperPrivacyPolicyVersion,
                RequiresTermsAndConditions = RequiresTermsAndConditions,
                DeveloperTermsAndConditionsUrl = DeveloperTermsAndConditionsUrl,
                DeveloperTermsAndConditionsVersion = DeveloperTermsAndConditionsVersion,
                TenantUuid = TenantUuid,
                Name = Name,
                SubdomainPattern = SubdomainPattern,
                LogoSvgUrl = LogoSvgUrl,
                LogoPngUrl = LogoPngUrl,
                IconIcoUrl = IconIcoUrl,
                StorageImplementation = StorageImplementation,
                StorageConnectionString = StorageConnectionString,
                PrimaryColor = PrimaryColor,
                SecondaryColor = SecondaryColor,
                TextOnPrimaryColor = TextOnPrimaryColor,
                TextOnSecondaryColor = TextOnSecondaryColor,
                PrimaryColorHex = PrimaryColorHex,
                SecondaryColorHex = SecondaryColorHex,
                TextOnPrimaryColorHex = TextOnPrimaryColorHex,
                TextOnSecondaryColorHex = TextOnSecondaryColorHex,
                SupportEmail = SupportEmail,
                NoreplyEmail = NoreplyEmail,
                CustomInvitationEmailTextPrefix = CustomInvitationEmailTextPrefix,
                CustomInvitationEmailTextSuffix = CustomInvitationEmailTextSuffix,
                ProductName = ProductName,
                DefaultCulture = DefaultCulture,
                DefaultCurrency = DefaultCurrency,
                BackendApiUrl = BackendApiUrl,
                FrontendApiUrl = FrontendApiUrl,
                WebUrl = WebUrl,
                UsersAreExternallyManaged = UsersAreExternallyManaged,
                ExternalUsersAreManuallyManaged = ExternalUsersAreManuallyManaged,
                ExternalAuthenticationMethod = ExternalAuthenticationMethod,
                OidcClientId = OidcClientId,
                OidcClientSecret = OidcClientSecret,
                OidcEndpointUrl = OidcEndpointUrl,
                SamlEntityId = SamlEntityId,
                SamlPeerEntityId = SamlPeerEntityId,
                SamlPeerIdpMetadataLocation = SamlPeerIdpMetadataLocation,
                SamlPeerIdpMetadata = SamlPeerIdpMetadata,
                SamlCertificate = SamlCertificate,
                SamlAllowWeakSigningAlgorithm = SamlAllowWeakSigningAlgorithm,
                ExternalDirectoryType = ExternalDirectoryType,
                ExternalDirectoryHost = ExternalDirectoryHost,
                ExternalDirectoryPort = ExternalDirectoryPort,
                ExternalDirectoryUsesSsl = ExternalDirectoryUsesSsl,
                ExternalDirectorySslCertificate = ExternalDirectorySslCertificate,
                ExternalDirectoryAccountDistinguishedName = ExternalDirectoryAccountDistinguishedName,
                ExternalDirectoryPassword = ExternalDirectoryPassword,
                ExternalDirectoryLoginAttribute = ExternalDirectoryLoginAttribute,
                ExternalDirectoryBaseContexts = ExternalDirectoryBaseContexts,
                ExternalDirectoryUserFilter = ExternalDirectoryUserFilter,
                ExternalDirectoryGroupFilter = ExternalDirectoryGroupFilter,
                ExternalDirectorySyncIntervalSeconds = ExternalDirectorySyncIntervalSeconds,
                ExternalDirectoryAdministratorGroupUuid = ExternalDirectoryAdministratorGroupUuid,
                CustomTenantSettingsJson = CustomTenantSettingsJson,
                AdditionalCacheKey = AdditionalCacheKey,
                RequiresDevAdminSsoReplacement = RequiresDevAdminSsoReplacement,
                DevAdminSsoReplacementSamlPeerEntityId = DevAdminSsoReplacementSamlPeerEntityId,
                DevAdminSsoReplacementSamlPeerIdpMetadataLocation = DevAdminSsoReplacementSamlPeerIdpMetadataLocation,
                CreatedByUserUuid = CreatedByUserUuid,
                Version = Version,
                CreatedAt = CreatedAt,
                LastUpdatedAt = LastUpdatedAt
            };
        }
    }
}
