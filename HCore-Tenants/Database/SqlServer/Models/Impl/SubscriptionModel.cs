using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HCore.Web.API.Impl;

namespace HCore.Tenants.Database.SqlServer.Models.Impl
{
    [Serializable]
    public class SubscriptionModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Uuid { get; set; }

        public string[] SubscriptionKeys { get; set; }
        public string[] FeatureKeys { get; set; }
        
        public string[] ExtraSubscriptionKeys { get; set; }
        public string[] ExtraFeatureKeys { get; set; }

        public string Scope { get; set; }
        public long[] ScopeUuids { get; set; }

        [StringLength(ApiImpl.MaxNameLength)]
        public string Name { get; set; }

        [StringLength(ApiImpl.MaxCommentLength)]
        public string Comment { get; set; }

        [StringLength(ApiImpl.MaxExternalUuidLength)]
        public string ExternalSubscriptionUuid { get; set; }
        public int? ExternalSubscriptionVersion { get; set; }

        public bool? IsActive { get; set; }
        public bool? IsLocked { get; set; }

        [StringLength(ApiImpl.MaxCommentLength)]
        public string LockReason { get; set; }

        public bool? IsOverdue { get; set; }

        public DateTimeOffset? IsOverdueSince { get; set; }

        public DateTimeOffset? ExpiresAt { get; set; }

        public long? Price { get; set; }
        public CurrencyEnum? Currency { get; set; }

        public SubscriptionBillingTypeEnum? BillingType { get; set; }

        [StringLength(ApiImpl.MaxNameLength)]
        public string CompanyName { get; set; }
        [StringLength(ApiImpl.MaxAddressLineLength)]
        public string AddressLine1 { get; set; }
        [StringLength(ApiImpl.MaxAddressLineLength)]
        public string AddressLine2 { get; set; }
        [StringLength(ApiImpl.MaxPostalCodeLength)]
        public string PostalCode { get; set; }
        [StringLength(ApiImpl.MaxCityLength)]
        public string City { get; set; }
        [StringLength(ApiImpl.MaxStateLength)]
        public string State { get; set; }
        [StringLength(ApiImpl.MaxCountryCodeLength)]
        public string CountryCode { get; set; }
        [StringLength(ApiImpl.MaxVatIdLength)]
        public string VatId { get; set; }
        [StringLength(ApiImpl.MaxContactPersonNameLength)]
        public string ContactPersonName { get; set; }
        [StringLength(ApiImpl.MaxEmailAddressLength)]
        public string ContactPersonEmailAddress { get; set; }

        public long TenantUuid { get; set; }

        [field: NonSerialized]
        public TenantModel Tenant { get; set; }

        [ConcurrencyCheck]
        public int Version { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastUpdatedAt { get; set; }
    }
}
