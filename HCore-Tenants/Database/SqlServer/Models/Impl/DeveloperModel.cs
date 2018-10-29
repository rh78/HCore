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
        public const int MaxAuthCookieDomainLength = 255;
        
        [Key]
        public long Uuid { get; set; }

        [StringLength(MaxHostPatternLength)]
        public string HostPattern { get; set; }

        [StringLength(MaxAuthorityLength)]
        public string Authority { get; set; }
        
        [StringLength(MaxAuthCookieDomainLength)]
        public string AuthCookieDomain { get; set; }

        public byte[] Certificate { get; set; }

        public List<TenantModel> Tenants { get; set; }
        
        public long Version { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? LastUpdated { get; set; }
    }
}
