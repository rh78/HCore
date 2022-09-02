using System;
using System.ComponentModel.DataAnnotations;

namespace HCore.Identity.Database.SqlServer.Models.Impl
{
    public class UserDeletedModel
    {
        [Key]
        [StringLength(Web.API.Impl.ApiImpl.MaxExternalUuidLength)]
        public string ExternalUuid { get; set; }

        public long? DeveloperUuid { get; set; }
        public long? TenantUuid { get; set; }

        public long? AuthScopeConfigurationUuid { get; set; }

        public DateTimeOffset? DeletedAt { get; set; }

        [StringLength(Web.API.Impl.ApiImpl.MaxExternalUuidLength)]
        public string DeletedByUserUuid { get; set; }
    }
}
