namespace HCore.Identity.Database.SqlServer.Models.Impl
{
    public class ReservedEmailAddressModel
    {
        public string Uuid { get; set; }
        public string NormalizedEmailAddress { get; set; }

        public long? DeveloperUuid { get; set; }
        public long? TenantUuid { get; set; }

        public long? AuthScopeConfigurationUuid { get; set; }
    }
}
