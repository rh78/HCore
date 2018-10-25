namespace HCore.Tenants
{
    public interface ITenantInfo
    {
        long DeveloperUuid { get; }
        string DeveloperAuthority { get; }
        byte[] DeveloperCertificate { get; }

        long TenantUuid { get; }
        string Name { get; }
        string LogoUrl { get; }
    }
}
