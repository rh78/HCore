namespace HCore.Tenants.Models
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

        int PrimaryColor { get; }
        int SecondaryColor { get; }
        int TextOnPrimaryColor { get; }
        int TextOnSecondaryColor { get; }

        string SupportEmail { get; }
        string NoreplyEmail { get; }

        string ProductName { get; }

        string ApiUrl { get; }
        string WebUrl { get; }
    }
}
