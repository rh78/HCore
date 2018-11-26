namespace HCore.Tenants.Models
{
    public interface IDeveloperInfo
    {
        long DeveloperUuid { get; }
        string Authority { get; }
        string Audience { get; }
        byte[] Certificate { get; }
        string CertificatePassword { get; }
        string AuthCookieDomain { get; }

        string Name { get; }

        string LogoSvgUrl { get; }
        string LogoPngUrl { get; }
        string IconIcoUrl { get; }

        int PrimaryColor { get; }
        int SecondaryColor { get; }
        int TextOnPrimaryColor { get; }
        int TextOnSecondaryColor { get; }

        string SupportEmail { get; }
        string NoreplyEmail { get; }

        string ProductName { get; }
    }
}
