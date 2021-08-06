using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HCore.Tenants.Database.SqlServer.Models.Impl
{
    public class DeveloperModel
    {
        public const int MaxHostPatternLength = 255;
        public const int MaxAuthorityLength = 255;
        public const int MaxAudienceLength = 255;
        public const int MaxAuthCookieDomainLength = 255;
        public const int MaxCertificatePasswordLength = 255;
        public const int MaxNameLength = 50;
        public const int MaxUrlLength = 255;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Uuid { get; set; }

        [StringLength(MaxHostPatternLength)]
        public string HostPattern { get; set; }

        [StringLength(MaxAuthorityLength)]
        public string Authority { get; set; }

        [StringLength(MaxAudienceLength)]
        public string Audience { get; set; }
        
        [StringLength(MaxAuthCookieDomainLength)]
        public string AuthCookieDomain { get; set; }

        [StringLength(MaxUrlLength)]
        public string DefaultEcbBackendApiUrlSuffix { get; set; }

        [StringLength(MaxUrlLength)]
        public string DefaultPortalsBackendApiUrlSuffix { get; set; }

        [StringLength(MaxUrlLength)]
        public string DefaultFrontendApiUrlSuffix { get; set; }
        [StringLength(MaxUrlLength)]
        public string DefaultWebUrlSuffix { get; set; }

        public string PrivacyPolicyUrl { get; set; }
        public int? PrivacyPolicyVersion { get; set; }

        public bool RequiresTermsAndConditions { get; set; }

        public string TermsAndConditionsUrl { get; set; }
        public int? TermsAndConditionsVersion { get; set; }

        public string Certificate { get; set; }

        [StringLength(MaxCertificatePasswordLength)]
        public string CertificatePassword { get; set; }

        [StringLength(MaxNameLength)]
        public string Name { get; set; }

        [StringLength(MaxUrlLength)]
        public string LogoSvgUrl { get; set; }

        [StringLength(MaxUrlLength)]
        public string LogoPngUrl { get; set; }

        [StringLength(MaxUrlLength)]
        public string IconIcoUrl { get; set; }

        public string StorageImplementation { get; set; }
        public string StorageConnectionString { get; set; }

        // see https://material.io/tools/color

        public int PrimaryColor { get; set; }
        public int SecondaryColor { get; set; }
        public int TextOnPrimaryColor { get; set; }
        public int TextOnSecondaryColor { get; set; }

        public string SupportEmail { get; set; }
        public string SupportEmailDisplayName { get; set; }

        public string NoreplyEmail { get; set; }
        public string NoreplyEmailDisplayName { get; set; }

        public string EmailSettingsJson { get; set; }

        public string EcbProductName { get; set; }
        public string PortalsProductName { get; set; }

        public List<TenantModel> Tenants { get; set; }
        
        public int Version { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastUpdatedAt { get; set; }

        public void SetEmailSettings(EmailSettingsModel emailSettingsModel)
        {
            EmailSettingsJson = JsonConvert.SerializeObject(emailSettingsModel);
        }

        public EmailSettingsModel GetEmailSettings()
        {
            if (EmailSettingsJson == null)
                return default;

            return JsonConvert.DeserializeObject<EmailSettingsModel>(EmailSettingsJson);
        }
    }
}
