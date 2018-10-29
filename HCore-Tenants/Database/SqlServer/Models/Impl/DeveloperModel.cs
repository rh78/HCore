using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HCore.Tenants.Database.SqlServer.Models.Impl
{
    public class DeveloperModel
    {
        public const int MaxHostPatternLength = 255;
        public const int MaxAuthorityLength = 255;
        public const int MaxAudienceLength = 255;
        public const int MaxAuthCookieDomainLength = 255;
        public const int MaxCertificatePasswordLength = 255;
        
        [Key]
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

        public List<TenantModel> Tenants { get; set; }
        
        public long Version { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? LastUpdated { get; set; }
    }
}
