using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HCore.Tenants.Database.SqlServer.Models.Impl
{
    public class TenantModel
    {
        public const int MaxSubdomainPatternLength = 255;
        public const int MaxNameLength = 50;
        public const int MaxLogoUrlLength = 50;
        public const int MaxUrlLength = 255;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Uuid { get; set; }

        public DeveloperModel Developer { get; set; }

        [StringLength(MaxSubdomainPatternLength)]
        public string SubdomainPattern { get; set; }

        [StringLength(MaxUrlLength)]
        public string ApiUrl { get; set; }

        [StringLength(MaxUrlLength)]
        public string WebUrl { get; set; }

        [StringLength(MaxNameLength)]
        public string Name { get; set; }

        [StringLength(MaxLogoUrlLength)]
        public string LogoUrl { get; set; }        
        
        public long Version { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastUpdatedAt { get; set; }
    }
}
