using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using HCore.Identity;
using IdentityModel;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace HCore.Test
{
    // validates tokens with the parameters HCore sets per tenant for JwtBearer, so a Microsoft.IdentityModel update that changes claims shows here
    public class IdentityModelClaimsTests
    {
        private const string Authority = "https://auth.example.com";
        private const string OtherAuthority = "https://other-auth.example.com";
        private const string CertificateSubject = "CN=HCore.Test";

        private const string UserUuid = "4711";
        private const string Email = "user@example.com";
        private const string DeveloperUuid = "1001";
        private const string ReadScope = "api.read";
        private const string WriteScope = "api.write";

        private const int ExpiredWithinClockSkewMinutes = 1;
        private const int ExpiredBeyondClockSkewMinutes = 10;

        [Fact]
        public async Task ValidToken_KeepsTheClaimNamesAsIssued()
        {
            using var certificate = CreateCertificate();

            var token = CreateToken(certificate, Authority, DateTime.UtcNow.AddHours(1));

            var validationParameters = CreateValidationParameters(certificate, Authority);

            var tokenHandler = new JsonWebTokenHandler();

            var validationResult = await tokenHandler.ValidateTokenAsync(token, validationParameters);

            Assert.True(validationResult.IsValid);

            var user = new ClaimsPrincipal(validationResult.ClaimsIdentity);

            Assert.Equal(UserUuid, user.GetUserUuid());
            Assert.Equal(Email, user.GetEmailAddress());
            Assert.Equal(DeveloperUuid, user.FindFirstValue(IdentityCoreConstants.DeveloperUuidClientClaim));
            Assert.Null(user.FindFirst(ClaimTypes.NameIdentifier));
        }

        private static X509Certificate2 CreateCertificate()
        {
            using var rsa = RSA.Create(2048);

            var certificateRequest = new CertificateRequest(CertificateSubject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            return certificateRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        }

        private static string CreateToken(X509Certificate2 certificate, string issuer, DateTime expires)
        {
            var signingKey = new X509SecurityKey(certificate);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = issuer,
                IssuedAt = expires.AddHours(-2),
                NotBefore = expires.AddHours(-2),
                Expires = expires,
                Claims = new Dictionary<string, object>
                {
                    { JwtClaimTypes.Subject, UserUuid },
                    { JwtClaimTypes.Email, Email },
                    { IdentityCoreConstants.DeveloperUuidClientClaim, DeveloperUuid },
                    { JwtClaimTypes.Scope, new[] { ReadScope, WriteScope } }
                },
                SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256)
            };

            var tokenHandler = new JsonWebTokenHandler();

            return tokenHandler.CreateToken(tokenDescriptor);
        }

        // the per-tenant JwtBearer options of IdentityServiceCollectionExtensions.ConfigureJwtAuthentication, with the developer certificate of the tenant
        private static TokenValidationParameters CreateValidationParameters(X509Certificate2 certificate, string authority)
        {
            return new TokenValidationParameters
            {
                ClockSkew = TimeSpan.FromMinutes(5),
                RequireSignedTokens = true,
                RequireExpirationTime = true,
                ValidateLifetime = true,
                ValidateAudience = false,
                ValidateIssuer = true,
                ValidIssuer = authority,
                IssuerSigningKey = new X509SecurityKey(certificate)
            };
        }

        [Fact]
        public async Task ScopeArray_BecomesOneClaimPerScope()
        {
            using var certificate = CreateCertificate();

            var token = CreateToken(certificate, Authority, DateTime.UtcNow.AddHours(1));

            var validationParameters = CreateValidationParameters(certificate, Authority);

            var tokenHandler = new JsonWebTokenHandler();

            var validationResult = await tokenHandler.ValidateTokenAsync(token, validationParameters);

            var scopes = validationResult.ClaimsIdentity.Claims
                .Where(claim => claim.Type == JwtClaimTypes.Scope)
                .Select(claim => claim.Value)
                .ToList();

            Assert.Equal(new[] { ReadScope, WriteScope }, scopes);
        }

        [Fact]
        public async Task FindFirst_MatchesTheClaimTypeCaseSensitively()
        {
            using var certificate = CreateCertificate();

            var token = CreateToken(certificate, Authority, DateTime.UtcNow.AddHours(1));

            var validationParameters = CreateValidationParameters(certificate, Authority);

            var tokenHandler = new JsonWebTokenHandler();

            var validationResult = await tokenHandler.ValidateTokenAsync(token, validationParameters);

            var user = new ClaimsPrincipal(validationResult.ClaimsIdentity);

            var upperCaseSubjectType = JwtClaimTypes.Subject.ToUpperInvariant();

            Assert.NotNull(user.FindFirst(JwtClaimTypes.Subject));
            Assert.Null(user.FindFirst(upperCaseSubjectType));
        }

        [Fact]
        public async Task TokenSignedWithAnotherTenantsCertificate_IsRejected()
        {
            using var certificate = CreateCertificate();
            using var otherCertificate = CreateCertificate();

            var token = CreateToken(otherCertificate, Authority, DateTime.UtcNow.AddHours(1));

            var validationParameters = CreateValidationParameters(certificate, Authority);

            var tokenHandler = new JsonWebTokenHandler();

            var validationResult = await tokenHandler.ValidateTokenAsync(token, validationParameters);

            Assert.False(validationResult.IsValid);
        }

        [Fact]
        public async Task TokenOfAnotherIssuer_IsRejected()
        {
            using var certificate = CreateCertificate();

            var token = CreateToken(certificate, OtherAuthority, DateTime.UtcNow.AddHours(1));

            var validationParameters = CreateValidationParameters(certificate, Authority);

            var tokenHandler = new JsonWebTokenHandler();

            var validationResult = await tokenHandler.ValidateTokenAsync(token, validationParameters);

            Assert.False(validationResult.IsValid);
        }

        [Theory]
        [InlineData(ExpiredWithinClockSkewMinutes, true)]
        [InlineData(ExpiredBeyondClockSkewMinutes, false)]
        public async Task ExpiredToken_IsValidOnlyWithinTheClockSkew(int expiredMinutesAgo, bool expectedValid)
        {
            using var certificate = CreateCertificate();

            var expires = DateTime.UtcNow.AddMinutes(-expiredMinutesAgo);

            var token = CreateToken(certificate, Authority, expires);

            var validationParameters = CreateValidationParameters(certificate, Authority);

            var tokenHandler = new JsonWebTokenHandler();

            var validationResult = await tokenHandler.ValidateTokenAsync(token, validationParameters);

            Assert.Equal(expectedValid, validationResult.IsValid);
        }
    }
}
