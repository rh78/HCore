namespace HCore.Tenants
{
    public interface ITenantInfo
    {
        long DeveloperUuid { get; }
        string DeveloperAuthority { get; }
        string DeveloperAudience { get; }
        byte[] DeveloperCertificate { get; }
        string DeveloperCertificatePassword { get; }
        string DeveloperAuthCookieDomain { get; }

        long TenantUuid { get; }
        string Name { get; }
        string LogoUrl { get; }
        
        string ApiUrl { get; }
        string WebUrl { get; }
    }
}
