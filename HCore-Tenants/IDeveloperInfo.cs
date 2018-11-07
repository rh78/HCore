namespace HCore.Tenants
{
    public interface IDeveloperInfo
    {
        long DeveloperUuid { get; }
        string Authority { get; }
        string Audience { get; }
        byte[] Certificate { get; }
        string CertificatePassword { get; }
        string AuthCookieDomain { get; }    
    }
}
