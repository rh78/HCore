using System;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using HCore.Web.Helpers;
using Xunit;

namespace HCore.Test
{
    /// <summary>
    /// CertificateHelpers.LoadCertificate on every content type the stored certificates can have, and its fallback to the obsolete X509Certificate2 constructor
    /// </summary>
    public class CertificateHelpersTests
    {
        private const string CertificateSubject = "CN=HCore.Test.CertificateHelpers";
        private const string Password = "test-password";
        private const string WrongPassword = "wrong-password";

        private static readonly byte[] InvalidCertificateBytes = new byte[]
        {
            1,
            2,
            3,
            4
        };

        private static readonly byte[] SignedContent = new byte[]
        {
            10,
            20,
            30,
            40
        };

        [Fact]
        public void LoadCertificate_X509ContentTypeCert_ShouldLoad_WithoutPrivateKey()
        {
            using var sourceCertificate = CreateCertificate();

            var certificateBytes = sourceCertificate.Export(X509ContentType.Cert);

            using var certificate = CertificateHelpers.LoadCertificate(certificateBytes, Password);

            Assert.Equal(sourceCertificate.Thumbprint, certificate.Thumbprint);
            Assert.False(certificate.HasPrivateKey);
        }

        private static X509Certificate2 CreateCertificate()
        {
            using var rsa = RSA.Create(2048);

            var certificateRequest = new CertificateRequest(CertificateSubject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            return certificateRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        }

        [Fact]
        public void LoadCertificate_ExportCertificatePem_ShouldLoad_WithoutPrivateKey()
        {
            using var sourceCertificate = CreateCertificate();

            var certificatePem = sourceCertificate.ExportCertificatePem();

            var certificateBytes = Encoding.ASCII.GetBytes(certificatePem);

            using var certificate = CertificateHelpers.LoadCertificate(certificateBytes, password: null);

            Assert.Equal(sourceCertificate.Thumbprint, certificate.Thumbprint);
            Assert.False(certificate.HasPrivateKey);
        }

        [Fact]
        public void LoadCertificate_X509ContentTypePfxAndEmptyPassword_ShouldLoad_ThePrivateKey()
        {
            using var sourceCertificate = CreateCertificate();

            var certificateBytes = sourceCertificate.Export(X509ContentType.Pfx);

            using var certificate = CertificateHelpers.LoadCertificate(certificateBytes, string.Empty);

            Assert.Equal(sourceCertificate.Thumbprint, certificate.Thumbprint);
            Assert.True(certificate.HasPrivateKey);
        }

        [Fact]
        public void LoadCertificate_WrongPassword_ShouldThrow_CryptographicException()
        {
            using var sourceCertificate = CreateCertificate();

            var certificateBytes = sourceCertificate.Export(X509ContentType.Pfx, Password);

            Assert.ThrowsAny<CryptographicException>(() => CertificateHelpers.LoadCertificate(certificateBytes, WrongPassword));
        }

        [Theory]
        [InlineData(X509KeyStorageFlags.EphemeralKeySet, false)]
        [InlineData(X509KeyStorageFlags.DefaultKeySet, true)]
        public void LoadCertificate_KeyStorageFlags_ShouldSet_TheKeyName(X509KeyStorageFlags keyStorageFlags, bool isKeyNamed)
        {
            // only Windows keeps a persisted key in a named container, elsewhere both flags keep the key in memory

            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            using var sourceCertificate = CreateCertificate();

            var certificateBytes = sourceCertificate.Export(X509ContentType.Pfx, Password);

            using var certificate = CertificateHelpers.LoadCertificate(certificateBytes, Password, keyStorageFlags);

            using var privateKey = (RSACng)certificate.GetRSAPrivateKey();

            var keyName = privateKey.Key.KeyName;

            var isLoadedKeyNamed = !string.IsNullOrEmpty(keyName);

            Assert.Equal(isKeyNamed, isLoadedKeyNamed);
        }

        [Fact]
        public void LoadCertificate_InvalidCertificateBytes_ShouldThrow_CryptographicException()
        {
            Assert.ThrowsAny<CryptographicException>(() => CertificateHelpers.LoadCertificate(InvalidCertificateBytes, Password));
        }

        [Fact]
        public void LoadCertificate_SignedCms_ShouldReturn_TheCmsSignerCertificate()
        {
            using var sourceCertificate = CreateCertificate();

            var contentInfo = new ContentInfo(SignedContent);

            var signedCms = new SignedCms(contentInfo);

            var cmsSigner = new CmsSigner(sourceCertificate);

            signedCms.ComputeSignature(cmsSigner);

            var certificateBytes = signedCms.Encode();

            using var certificate = CertificateHelpers.LoadCertificate(certificateBytes, password: null);

            Assert.Equal(sourceCertificate.Thumbprint, certificate.Thumbprint);
        }
    }
}
