using HCore.Tenants.Database.SqlServer.Models.Impl;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace HCore.Tenants.Models.Impl
{
    [Serializable]
    internal class TenantInfoImpl : ITenantInfo
    {
        public static Dictionary<string, X509Certificate2> StaticCertificates = new Dictionary<string, X509Certificate2>();

        public long DeveloperUuid { get; internal set; }
        public string DeveloperAuthority { get; internal set; }
        public string DeveloperAudience { get; internal set; }
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
        public string AppleTouchIconUrl { get; internal set; }

        public string CustomCss { get; internal set; }

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
        public string SupportEmailDisplayName { get; internal set; }

        public Dictionary<string, string> PermissionDeniedSupportMessage { get; internal set; }

        public string NoreplyEmail { get; internal set; }
        public string NoreplyEmailDisplayName { get; internal set; }

        public EmailSettingsModel EmailSettings { get; set; }

        public string EcbProductName { get; internal set; }
        public string PortalsProductName { get; internal set; }

        public string DefaultCulture { get; internal set; }
        public string DefaultCurrency { get; internal set; }

        public List<SubscriptionModel> Subscriptions { get; internal set; }
        public List<string> FeatureKeys { get; internal set; }

        public string EcbBackendApiUrl { get; set; }
        public string PortalsBackendApiUrl { get; set; }

        public string FrontendApiUrl { get; set; }

        public string WebUrl { get; set; }

        public bool UsersAreExternallyManaged { get; set; }
        public bool ExternalUsersAreManuallyManaged { get; set; }

        public string ExternalAuthenticationMethod { get; set; }
        
        public bool ExternalAuthenticationAllowLocalLogin { get; set; }

        public bool ExternalAuthenticationAllowUserMerge { get; set; }

        public Dictionary<string, string> ExternalAuthenticationClaimMappings { get; set; }

        public string OidcClientId { get; set; }
        public string OidcClientSecret { get; set; }

        public string OidcEndpointUrl { get; set; }

        public bool OidcUsePkce { get; set; }

        public string[] OidcScopes { get; set; }

        public string OidcAcrValues { get; set; }

        public string OidcAcrValuesAppendix { get; set; }
        public bool OidcTriggerAcrValuesAppendixByUrlParameter { get; set; }

        public bool OidcQueryUserInfoEndpoint { get; set; }

        public Dictionary<string, string> OidcAdditionalParameters { get; set; }

        public string SamlEntityId { get; set; }

        public string SamlPeerEntityId { get; set; }

        public string SamlPeerIdpMetadataLocation { get; set; }
        public string SamlPeerIdpMetadata { get; set; }

        public bool SamlAllowWeakSigningAlgorithm { get; set; }

        public string ExternalDirectoryType { get; set; }
        public string ExternalDirectoryHost { get; set; }
        public int? ExternalDirectoryPort { get; set; }

        public bool? ExternalDirectoryUsesSsl { get; set; }

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

        public long? MapDeveloperUuid { get; set; }
        public long? MapTenantUuid { get; set; }
        
        public long? MapCustomUuid { get; set; }

        public bool? IsDraft { get; set; }

        public int Version { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastUpdatedAt { get; set; }

        public bool EnableAudit { get; set; }

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
                DeveloperCertificateBytes = DeveloperCertificateBytes,
                DeveloperCertificatePassword = DeveloperCertificatePassword,
                DeveloperCertificateThumbprint = DeveloperCertificateThumbprint,
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
                AppleTouchIconUrl = AppleTouchIconUrl,
                CustomCss = CustomCss,
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
                SupportEmailDisplayName = SupportEmailDisplayName,
                PermissionDeniedSupportMessage = PermissionDeniedSupportMessage,
                NoreplyEmail = NoreplyEmail,
                NoreplyEmailDisplayName = NoreplyEmailDisplayName,
                EmailSettings = EmailSettings,
                EcbProductName = EcbProductName,
                PortalsProductName = PortalsProductName,
                DefaultCulture = DefaultCulture,
                DefaultCurrency = DefaultCurrency,
                Subscriptions = Subscriptions,
                FeatureKeys = FeatureKeys,
                HttpsCertificateBytes = HttpsCertificateBytes,
                HttpsCertificatePassword = HttpsCertificatePassword,
                HttpsCertificateThumbprint = HttpsCertificateThumbprint,
                EcbBackendApiUrl = EcbBackendApiUrl,
                PortalsBackendApiUrl = PortalsBackendApiUrl,
                FrontendApiUrl = FrontendApiUrl,
                WebUrl = WebUrl,
                UsersAreExternallyManaged = UsersAreExternallyManaged,
                ExternalUsersAreManuallyManaged = ExternalUsersAreManuallyManaged,
                ExternalAuthenticationMethod = ExternalAuthenticationMethod,
                ExternalAuthenticationAllowLocalLogin = ExternalAuthenticationAllowLocalLogin,
                ExternalAuthenticationAllowUserMerge = ExternalAuthenticationAllowUserMerge,
                ExternalAuthenticationClaimMappings = ExternalAuthenticationClaimMappings,
                OidcClientId = OidcClientId,
                OidcClientSecret = OidcClientSecret,
                OidcEndpointUrl = OidcEndpointUrl,
                OidcUsePkce = OidcUsePkce,
                OidcScopes = OidcScopes,
                OidcAcrValues = OidcAcrValues,
                OidcAcrValuesAppendix = OidcAcrValuesAppendix,
                OidcTriggerAcrValuesAppendixByUrlParameter = OidcTriggerAcrValuesAppendixByUrlParameter,
                OidcQueryUserInfoEndpoint = OidcQueryUserInfoEndpoint,
                OidcAdditionalParameters = OidcAdditionalParameters,
                SamlEntityId = SamlEntityId,
                SamlPeerEntityId = SamlPeerEntityId,
                SamlPeerIdpMetadataLocation = SamlPeerIdpMetadataLocation,
                SamlPeerIdpMetadata = SamlPeerIdpMetadata,
                SamlCertificateBytes = SamlCertificateBytes,
                SamlCertificatePassword = SamlCertificatePassword,
                SamlCertificateThumbprint = SamlCertificateThumbprint,
                SamlAllowWeakSigningAlgorithm = SamlAllowWeakSigningAlgorithm,
                ExternalDirectoryType = ExternalDirectoryType,
                ExternalDirectoryHost = ExternalDirectoryHost,
                ExternalDirectoryPort = ExternalDirectoryPort,
                ExternalDirectoryUsesSsl = ExternalDirectoryUsesSsl,
                ExternalDirectorySslCertificateBytes = ExternalDirectorySslCertificateBytes,
                ExternalDirectorySslCertificatePassword = ExternalDirectorySslCertificatePassword,
                ExternalDirectorySslCertificateThumbprint = ExternalDirectorySslCertificateThumbprint,
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
                EnableAudit = EnableAudit,
                CreatedByUserUuid = CreatedByUserUuid,
                MapDeveloperUuid = MapDeveloperUuid,
                MapTenantUuid = MapTenantUuid,
                MapCustomUuid = MapCustomUuid,
                Version = Version,
                CreatedAt = CreatedAt,
                LastUpdatedAt = LastUpdatedAt
            };
        }

        public byte[] DeveloperCertificateBytes { get; internal set; }
        public string DeveloperCertificatePassword { get; internal set; }
        public string DeveloperCertificateThumbprint { get; internal set; }

        public void SetDeveloperCertificate(byte[] certificateBytes, string certificatePassword)
        {
            if (certificateBytes == null || certificateBytes.Length == 0)
                return;

            DeveloperCertificateBytes = certificateBytes;
            DeveloperCertificatePassword = certificatePassword;

            X509Certificate2 certificate;

            if (string.IsNullOrEmpty(certificatePassword))
                certificate = new X509Certificate2(certificateBytes);
            else
                certificate = new X509Certificate2(certificateBytes, certificatePassword);

            DeveloperCertificateThumbprint = certificate.Thumbprint;

            StaticCertificates[certificate.Thumbprint] = certificate;
        }

        public X509Certificate2 GetDeveloperCertificate()
        {
            if (string.IsNullOrEmpty(DeveloperCertificateThumbprint))
                return null;
            
            if (!StaticCertificates.ContainsKey(DeveloperCertificateThumbprint))
            {
                SetDeveloperCertificate(DeveloperCertificateBytes, DeveloperCertificatePassword);
            }

            return StaticCertificates[DeveloperCertificateThumbprint];
        }

        public byte[] HttpsCertificateBytes { get; internal set; }
        public string HttpsCertificatePassword { get; internal set; }
        public string HttpsCertificateThumbprint { get; internal set; }

        public void SetHttpsCertificate(byte[] certificateBytes, string certificatePassword)
        {
            if (certificateBytes == null || certificateBytes.Length == 0)
                return;

            HttpsCertificateBytes = certificateBytes;
            HttpsCertificatePassword = certificatePassword;

            X509Certificate2 certificate;

            if (string.IsNullOrEmpty(certificatePassword))
                certificate = new X509Certificate2(certificateBytes);
            else
                certificate = new X509Certificate2(certificateBytes, certificatePassword);

            HttpsCertificateThumbprint = certificate.Thumbprint;

            StaticCertificates[certificate.Thumbprint] = certificate;
        }

        public X509Certificate2 GetHttpsCertificate()
        {
            if (string.IsNullOrEmpty(HttpsCertificateThumbprint))
                return null;

            if (!StaticCertificates.ContainsKey(HttpsCertificateThumbprint))
            {
                SetHttpsCertificate(HttpsCertificateBytes, HttpsCertificatePassword);
            }

            return StaticCertificates[HttpsCertificateThumbprint];
        }

        public byte[] SamlCertificateBytes { get; internal set; }
        public string SamlCertificatePassword { get; internal set; }
        public string SamlCertificateThumbprint { get; internal set; }

        public void SetSamlCertificate(byte[] certificateBytes, string certificatePassword)
        {
            if (certificateBytes == null || certificateBytes.Length == 0)
                return;

            SamlCertificateBytes = certificateBytes;
            SamlCertificatePassword = certificatePassword;

            X509Certificate2 certificate;

            if (string.IsNullOrEmpty(certificatePassword))
                certificate = new X509Certificate2(certificateBytes);
            else
                certificate = new X509Certificate2(certificateBytes, certificatePassword);

            SamlCertificateThumbprint = certificate.Thumbprint;

            StaticCertificates[certificate.Thumbprint] = certificate;
        }

        public X509Certificate2 GetSamlCertificate()
        {
            if (string.IsNullOrEmpty(SamlCertificateThumbprint))
                return null;

            if (!StaticCertificates.ContainsKey(SamlCertificateThumbprint))
            {
                SetSamlCertificate(SamlCertificateBytes, SamlCertificatePassword);
            }

            return StaticCertificates[SamlCertificateThumbprint];
        }

        public byte[] ExternalDirectorySslCertificateBytes { get; internal set; }
        public string ExternalDirectorySslCertificatePassword { get; internal set; }
        public string ExternalDirectorySslCertificateThumbprint { get; internal set; }

        public void SetExternalDirectorySslCertificate(byte[] certificateBytes, string certificatePassword)
        {
            if (certificateBytes == null || certificateBytes.Length == 0)
                return;

            ExternalDirectorySslCertificateBytes = certificateBytes;
            ExternalDirectorySslCertificatePassword = certificatePassword;

            X509Certificate2 certificate;

            if (string.IsNullOrEmpty(certificatePassword))
                certificate = new X509Certificate2(certificateBytes);
            else
                certificate = new X509Certificate2(certificateBytes, certificatePassword);

            ExternalDirectorySslCertificateThumbprint = certificate.Thumbprint;

            StaticCertificates[certificate.Thumbprint] = certificate;
        }

        public X509Certificate2 GetExternalDirectorySslCertificate()
        {
            if (string.IsNullOrEmpty(ExternalDirectorySslCertificateThumbprint))
                return null;

            if (!StaticCertificates.ContainsKey(ExternalDirectorySslCertificateThumbprint))
            {
                SetExternalDirectorySslCertificate(ExternalDirectorySslCertificateBytes, ExternalDirectorySslCertificatePassword);
            }

            return StaticCertificates[ExternalDirectorySslCertificateThumbprint];
        }
    }
}
