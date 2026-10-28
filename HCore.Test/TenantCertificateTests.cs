using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using HCore.Tenants.Models.Impl;
using Xunit;

namespace HCore.Test
{
    // the certificate loading of TenantInfoImpl, so the switch from the X509Certificate2 constructors to X509CertificateLoader shows here
    public class TenantCertificateTests
    {
        private const string CertificateSubject = "CN=HCore.Test.Tenant";
        private const string Password = "test-password";

        public enum CertificateKind
        {
            Developer,
            Https,
            Saml
        }

        [Theory]
        [InlineData(CertificateKind.Developer, Password)]
        [InlineData(CertificateKind.Developer, null)]
        [InlineData(CertificateKind.Https, Password)]
        [InlineData(CertificateKind.Https, null)]
        [InlineData(CertificateKind.Saml, Password)]
        [InlineData(CertificateKind.Saml, null)]
        public void Certificate_LoadsWithItsPrivateKey(CertificateKind certificateKind, string password)
        {
            using var sourceCertificate = CreateCertificate();

            var certificateBytes = sourceCertificate.Export(X509ContentType.Pfx, password);

            var tenantInfo = new TenantInfoImpl();

            SetCertificate(tenantInfo, certificateKind, certificateBytes, password);

            var certificate = GetCertificate(tenantInfo, certificateKind);

            Assert.Equal(sourceCertificate.Thumbprint, certificate.Thumbprint);
            Assert.True(certificate.HasPrivateKey);
        }

        private static X509Certificate2 CreateCertificate()
        {
            using var rsa = RSA.Create(2048);

            var certificateRequest = new CertificateRequest(CertificateSubject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            return certificateRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        }

        private static void SetCertificate(TenantInfoImpl tenantInfo, CertificateKind certificateKind, byte[] certificateBytes, string password)
        {
            switch (certificateKind)
            {
                case CertificateKind.Developer:
                    tenantInfo.SetDeveloperCertificate(certificateBytes, password);

                    break;
                case CertificateKind.Https:
                    tenantInfo.SetHttpsCertificate(certificateBytes, password);

                    break;
                case CertificateKind.Saml:
                    tenantInfo.SetSamlCertificate(certificateBytes, password);

                    break;
            }
        }

        private static X509Certificate2 GetCertificate(TenantInfoImpl tenantInfo, CertificateKind certificateKind)
        {
            var certificate = certificateKind switch
            {
                CertificateKind.Developer => tenantInfo.GetDeveloperCertificate(),
                CertificateKind.Https => tenantInfo.GetHttpsCertificate(),
                CertificateKind.Saml => tenantInfo.GetSamlCertificate(),
                _ => null
            };

            return certificate;
        }

        [Theory]
        [InlineData(CertificateKind.Developer)]
        [InlineData(CertificateKind.Https)]
        [InlineData(CertificateKind.Saml)]
        public void CertificateMissingFromTheStaticCache_IsLoadedAgainFromItsBytes(CertificateKind certificateKind)
        {
            using var sourceCertificate = CreateCertificate();

            var certificateBytes = sourceCertificate.Export(X509ContentType.Pfx, Password);

            var tenantInfo = new TenantInfoImpl();

            SetCertificate(tenantInfo, certificateKind, certificateBytes, Password);

            TenantInfoImpl.StaticCertificates.Remove(sourceCertificate.Thumbprint);

            var certificate = GetCertificate(tenantInfo, certificateKind);

            Assert.Equal(sourceCertificate.Thumbprint, certificate.Thumbprint);
            Assert.True(certificate.HasPrivateKey);
        }

        [Theory]
        [InlineData(CertificateKind.Developer)]
        [InlineData(CertificateKind.Https)]
        [InlineData(CertificateKind.Saml)]
        public void EmptyCertificateBytes_LeaveNoCertificate(CertificateKind certificateKind)
        {
            var tenantInfo = new TenantInfoImpl();

            SetCertificate(tenantInfo, certificateKind, [], Password);

            var certificate = GetCertificate(tenantInfo, certificateKind);

            Assert.Null(certificate);
        }
    }
}
