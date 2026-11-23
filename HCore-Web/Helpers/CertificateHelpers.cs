using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace HCore.Web.Helpers
{
    /// <summary>
    /// Loads an X509Certificate2 from stored certificate bytes, PKCS#12 or a plain certificate
    /// </summary>
    public static class CertificateHelpers
    {
        /// <summary>
        /// the X509Certificate2 constructors detected the format themselves, X509CertificateLoader needs it named, so a stored public certificate keeps loading
        /// </summary>
        public static X509Certificate2 LoadCertificate(byte[] certificateBytes, string password, X509KeyStorageFlags keyStorageFlags = X509KeyStorageFlags.DefaultKeySet, ILogger logger = null)
        {
            var contentType = X509Certificate2.GetCertContentType(certificateBytes);

            try
            {
                if (contentType != X509ContentType.Pkcs12)
                {
                    return X509CertificateLoader.LoadCertificate(certificateBytes);
                }

                var pkcs12Password = !string.IsNullOrEmpty(password)
                    ? password
                    : null;

                return X509CertificateLoader.LoadPkcs12(certificateBytes, pkcs12Password, keyStorageFlags);
            }
            catch (CryptographicException e)
            {
                if (logger != null)
                {
                    logger.LogWarning(e, "X509CertificateLoader could not load the {ContentType} certificate, falling back to the obsolete X509Certificate2 constructor", contentType);
                }
                else
                {
                    Console.WriteLine($"X509CertificateLoader could not load the {contentType} certificate, falling back to the obsolete X509Certificate2 constructor. {e.Message}");
                }

                // a format X509CertificateLoader does not read, such as a signed PKCS#7, still loads as before the switch

#pragma warning disable SYSLIB0057
                return new X509Certificate2(certificateBytes, password, keyStorageFlags);
#pragma warning restore SYSLIB0057
            }
        }
    }
}
