using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.ComponentModel.DataAnnotations;

namespace HCore.Tenants.Database.SqlServer.Models.Impl
{
    public class TenantModel
    {
        public const int MaxSubdomainPatternLength = 255;
        public const int MaxNameLength = 50;
        public const int MaxLogoUrlLength = 50;        

        [Key]
        public long Uuid { get; set; }

        public DeveloperModel Developer { get; set; }

        [StringLength(MaxSubdomainPatternLength)]
        public string SubdomainPattern { get; set; }

        [StringLength(MaxNameLength)]
        public string Name { get; set; }

        [StringLength(MaxLogoUrlLength)]
        public string LogoUrl { get; set; }        
        
        public long Version { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? LastUpdated { get; set; }
    }
}
