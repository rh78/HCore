using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
        public const int MaxLogoUrlLength = 255;

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

        public byte[] Certificate { get; set; }

        [StringLength(MaxCertificatePasswordLength)]
        public string CertificatePassword { get; set; }

        [StringLength(MaxNameLength)]
        public string Name { get; set; }

        [StringLength(MaxLogoUrlLength)]
        public string LogoSvgUrl { get; set; }

        [StringLength(MaxLogoUrlLength)]
        public string LogoPngUrl { get; set; }

        [StringLength(MaxLogoUrlLength)]
        public string IconIcoUrl { get; set; }

        public string StorageImplementation { get; set; }
        public string StorageConnectionString { get; set; }

        // see https://material.io/tools/color

        public int PrimaryColor { get; set; }
        public int SecondaryColor { get; set; }
        public int TextOnPrimaryColor { get; set; }
        public int TextOnSecondaryColor { get; set; }

        public string SupportEmail { get; set; }
        public string NoreplyEmail { get; set; }

        public string ProductName { get; set; }

        public List<TenantModel> Tenants { get; set; }
        
        public int Version { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastUpdatedAt { get; set; }
    }
}
