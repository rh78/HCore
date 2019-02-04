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

        string LogoSvgUrl { get; }
        string LogoPngUrl { get; }
        string IconIcoUrl { get; }

        string StorageImplementation { get; }
        string StorageConnectionString { get; }
        
        int PrimaryColor { get; }
        int SecondaryColor { get; }
        int TextOnPrimaryColor { get; }
        int TextOnSecondaryColor { get; }

        string PrimaryColorHex { get; }
        string SecondaryColorHex { get; }
        string TextOnPrimaryColorHex { get; }
        string TextOnSecondaryColorHex { get; }

        string SupportEmail { get; }
        string NoreplyEmail { get; }

        string ProductName { get; }

        string BackendApiUrl { get; }
        string FrontendApiUrl { get; }

        string WebUrl { get; }
    }
}
