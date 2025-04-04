﻿using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HCore.Tenants.Database.SqlServer.Models.Impl
{
    public class TenantModel
    {
        public const int MaxNameLength = 50;
        public const int MaxSubdomainPatternLength = 255;
        public const int MaxUrlLength = 255;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Uuid { get; set; }

        public long DeveloperUuid { get; set; }
        public DeveloperModel Developer { get; set; }

        public string[] SubdomainPatterns { get; set; }

        [StringLength(MaxUrlLength)]
        public string EcbBackendApiUrl { get; set; }

        [StringLength(MaxUrlLength)]
        public string PortalsBackendApiUrl { get; set; }

        [StringLength(MaxUrlLength)]
        public string FrontendApiUrl { get; set; }

        [StringLength(MaxUrlLength)]
        public string WebUrl { get; set; }

        [StringLength(MaxUrlLength)]
        public string AllowIFrameUrl { get; set; }

        [StringLength(DeveloperModel.MaxNameLength)]
        public string Name { get; set; }

        public bool? RequiresTermsAndConditions { get; set; }

        [StringLength(DeveloperModel.MaxUrlLength)]
        public string LogoSvgUrl { get; set; }

        [StringLength(DeveloperModel.MaxUrlLength)]
        public string LogoPngUrl { get; set; }

        [StringLength(DeveloperModel.MaxUrlLength)]
        public string IconIcoUrl { get; set; }

        [StringLength(DeveloperModel.MaxUrlLength)]
        public string AppleTouchIconUrl { get; set; }

        public string CustomCss { get; set; }

        public string StorageImplementation { get; set; }
        public string StorageConnectionString { get; set; }

        public int? PrimaryColor { get; set; }
        public int? SecondaryColor { get; set; }
        public int? TextOnPrimaryColor { get; set; }
        public int? TextOnSecondaryColor { get; set; }

        public string SupportEmail { get; set; }
        public string SupportEmailDisplayName { get; set; }

        [Column(TypeName = "jsonb")]
        public Dictionary<string, string> PermissionDeniedSupportMessage { get; set; }

        public string NoreplyEmail { get; set; }
        public string NoreplyEmailDisplayName { get; set; }

        public string CustomEmailSettingsJson { get; set; }
        
        public string EcbProductName { get; set; }
        public string PortalsProductName { get; set; }

        public string DefaultCulture { get; set; }
        public string DefaultCurrency { get; set; }

        public string HttpsCertificate { get; set; }
        public string HttpsCertificatePassword { get; set; }

        public string ExternalAuthenticationMethod { get; set; }

        public bool ExternalAuthenticationAllowLocalLogin { get; set; }

        public bool ExternalAuthenticationAllowUserMerge { get; set; }

        public string[] EnforceExternalAuthenticationForEmailDomains { get; set; }

        public string OidcClientId { get; set; }
        public string OidcClientSecret { get; set; }

        public string OidcEndpointUrl { get; set; }

        public string OidcEndSessionEndpointUrl { get; set; }

        public bool OidcUsePkce { get; set; }

        public string[] OidcScopes { get; set; }

        public string OidcAcrValues { get; set; }

        public string OidcAcrValuesAppendix { get; set; }
        public bool OidcTriggerAcrValuesAppendixByUrlParameter { get; set; }

        public bool OidcQueryUserInfoEndpoint { get; set; }

        [Column(TypeName = "jsonb")]
        public Dictionary<string, string> OidcAdditionalParameters { get; set; }

        public bool OidcUseStateRedirect { get; set; }
        public string OidcStateRedirectUrl { get; set; }
        public bool OidcStateRedirectNoProfile { get; set; }
        public string OidcOverridePostLogoutRedirectUrl { get; set; }

        [Column(TypeName = "jsonb")]
        public Dictionary<string, string> ExternalAuthenticationClaimMappings { get; set; }

        public string SamlEntityId { get; set; }

        public string SamlPeerEntityId { get; set; }

        public string SamlPeerIdpMetadataLocation { get; set; }
        public string SamlPeerIdpMetadata { get; set; }

        public string SamlCertificate { get; set; }
        public string SamlCertificatePassword { get; set; }

        public bool? SamlAllowWeakSigningAlgorithm { get; set; }

        public bool ExternalUsersAreManuallyManaged { get; set; }

        public string CustomTenantSettingsJson { get; set; }

        public bool RequiresDevAdminSsoReplacement { get; set; }

        public string DevAdminSsoReplacementOidcClientId { get; set; }
        public string DevAdminSsoReplacementOidcClientSecret { get; set; }

        public string DevAdminSsoReplacementOidcEndpointUrl { get; set; }

        public string DevAdminSsoReplacementSamlPeerEntityId { get; set; }
        public string DevAdminSsoReplacementSamlPeerIdpMetadataLocation { get; set; }

        public bool EnableAudit { get; set; }

        public string CreatedByUserUuid { get; set; }

        public long? MapDeveloperUuid { get; set; }
        public long? MapTenantUuid { get; set; }

        public long? MapCustomUuid { get; set; }

        public List<SubscriptionModel> Subscriptions { get; set; }

        [ConcurrencyCheck]
        public int Version { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastUpdatedAt { get; set; }

        public void SetCustomEmailSettings(CustomEmailSettingsModel customEmailSettingsModel)
        {
            CustomEmailSettingsJson = JsonConvert.SerializeObject(customEmailSettingsModel);
        }

        public CustomEmailSettingsModel GetCustomEmailSettings()
        {
            if (CustomEmailSettingsJson == null)
                return default;

            return JsonConvert.DeserializeObject<CustomEmailSettingsModel>(CustomEmailSettingsJson);
        }

        public void SetCustomTenantSettings<TCustomTenantSettingsDataType>(TCustomTenantSettingsDataType customTenantSettings)
        {
            CustomTenantSettingsJson = JsonConvert.SerializeObject(customTenantSettings);
        }

        public TCustomTenantSettingsDataType GetCustomTenantSettings<TCustomTenantSettingsDataType>()
        {
            if (CustomTenantSettingsJson == null)
                return default;

            return JsonConvert.DeserializeObject<TCustomTenantSettingsDataType>(CustomTenantSettingsJson);
        }

        public TenantModel Clone()
        {
            return JsonConvert.DeserializeObject<TenantModel>(JsonConvert.SerializeObject(this));
        }
    }
}
