using HCore.Tenants.Database.SqlServer.Models.Impl;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace HCore.Tenants.Models
{
    public interface ITenantInfo
    {
        long DeveloperUuid { get; }
        string DeveloperAuthority { get; }
        string DeveloperAudience { get; }
        string DeveloperAuthCookieDomain { get; }
        string DeveloperHostPattern { get; }
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
        string AppleTouchIconUrl { get; }

        string CustomCss { get; }
        string CustomEmailCss { get; }

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
        string SupportReplyToEmail { get; }
        string SupportEmailDisplayName { get; }

        Dictionary<string, string> PermissionDeniedSupportMessage { get; }

        string NoreplyEmail { get; }
        string NoreplyReplyToEmail { get; }
        string NoreplyEmailDisplayName { get; }

        string WebAddress { get; }
        string PoweredByShort { get; }

        bool HidePoweredBy { get; }

        EmailSettingsModel EmailSettings { get; }

        string EcbProductName { get; }
        string PortalsProductName { get; }

        string DefaultCulture { get; }
        string DefaultCurrency { get; }

        List<SubscriptionModel> Subscriptions { get; }
        List<string> FeatureKeys { get; }

        string EcbBackendApiUrl { get; }
        string PortalsBackendApiUrl { get; }

        string FrontendApiUrl { get; }

        string WebUrl { get; }

        bool UsersAreExternallyManaged { get; }
        bool ExternalUsersAreManuallyManaged { get; }

        string ExternalAuthenticationMethod { get; }

        bool ExternalAuthenticationAllowLocalLogin { get; }

        bool ExternalAuthenticationAllowUserMerge { get; }

        string[] EnforceExternalAuthenticationForEmailDomains { get; }

        Dictionary<string, string> ExternalAuthenticationClaimMappings { get; }

        string OidcClientId { get; set; }
        string OidcClientSecret { get; set; }

        string OidcEndpointUrl { get; set; }

        string OidcEndSessionEndpointUrl { get; }

        bool OidcUsePkce { get; set; }

        string[] OidcScopes { get; }

        string OidcAcrValues { get; }

        string OidcAcrValuesAppendix { get; }
        bool OidcTriggerAcrValuesAppendixByUrlParameter { get; }

        bool OidcQueryUserInfoEndpoint { get; }

        Dictionary<string, string> OidcAdditionalParameters { get; }

        bool OidcUseStateRedirect { get; }
        string OidcStateRedirectUrl { get; }
        bool OidcStateRedirectNoProfile { get; }
        string OidcOverridePostLogoutRedirectUrl { get; }

        string SamlEntityId { get; }

        string SamlPeerEntityId { get; set; }

        string SamlPeerIdpMetadataLocation { get; set; }
        string SamlPeerIdpMetadata { get; set; }

        bool SamlAllowWeakSigningAlgorithm { get; }

        string CustomTenantSettingsJson { get; }

        TCustomTenantSettingsDataType GetCustomTenantSettings<TCustomTenantSettingsDataType>();

        string AdditionalCacheKey { get; set; }

        bool RequiresDevAdminSsoReplacement { get; }

        public string DevAdminSsoReplacementOidcClientId { get; }
        public string DevAdminSsoReplacementOidcClientSecret { get; }

        public string DevAdminSsoReplacementOidcEndpointUrl { get; set; }

        string DevAdminSsoReplacementSamlPeerEntityId { get; }
        string DevAdminSsoReplacementSamlPeerIdpMetadataLocation { get; }

        bool EnableAudit { get; }

        public string CreatedByUserUuid { get; }

        public long? MapDeveloperUuid { get; }
        public long? MapTenantUuid { get; }

        public long? MapCustomUuid { get; }

        public int Version { get; }

        public DateTimeOffset CreatedAt { get; }
        public DateTimeOffset? LastUpdatedAt { get; }

        ITenantInfo Clone();

        public X509Certificate2 GetDeveloperCertificate();
        public X509Certificate2 GetHttpsCertificate();
        public X509Certificate2 GetSamlCertificate();
    }
}
