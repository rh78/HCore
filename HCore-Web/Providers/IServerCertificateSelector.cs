using System.Security.Cryptography.X509Certificates;

namespace HCore.Web.Providers
{
    public interface IServerCertificateSelector
    {
        X509Certificate2 GetServerCertificate(string hostName);
    }
}
