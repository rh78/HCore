namespace HCore.Tenants.Impl
{
    internal class DeveloperInfoImpl : IDeveloperInfo
    {
        public long DeveloperUuid { get; internal set; }
        public string Authority { get; internal set; }
        public string Audience { get; internal set; }
        public byte[] Certificate { get; internal set; }
        public string CertificatePassword { get; internal set; }
        public string AuthCookieDomain { get; internal set; }        
    }
}
