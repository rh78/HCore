using System;
using System.Collections.Generic;
using System.IO;
using HCore.Tenants.Database.SqlServer.Models.Impl;
using HCore.Tenants.Models;
using HCore.Tenants.Models.Impl;
using HCore.Tenants.Providers.Impl;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Xunit;

namespace HCore.Test
{
    public class RedisCrossVersionTests
    {
        private const string TenantInfoFixtureFileName = "tenant-info.msgpack";

        private const long DeveloperUuid = 1001;
        private const string DeveloperAuthority = "https://auth.example.com";
        private const string DeveloperName = "Example Developer";
        private const int DeveloperPrivacyPolicyVersion = 3;
        private const long TenantUuid = 2002;
        private const string TenantName = "Example Tenant";
        private const string SubdomainPattern = "example";
        private const int PrimaryColor = 0x112233;
        private const string PrimaryColorHex = "#112233";
        private const string SupportEmail = "support@example.com";
        private const string PermissionDeniedSupportMessageCulture = "en";
        private const string PermissionDeniedSupportMessage = "Ask your administrator for access.";
        private const long SubscriptionUuid = 3003;
        private const string SubscriptionKey = "portals";
        private const string FeatureKey = "audit";
        private const string OidcScope = "openid";
        private const int TenantVersion = 7;

        private static readonly byte[] DeveloperCertificateBytes = { 1, 2, 3, 4 };
        private static readonly DateTimeOffset CreatedAt = new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public void TenantInfo_SerializesToTheNet8Bytes()
        {
            ITenantInfo tenantInfo = new TenantInfoImpl
            {
                DeveloperUuid = DeveloperUuid,
                DeveloperAuthority = DeveloperAuthority,
                DeveloperName = DeveloperName,
                DeveloperPrivacyPolicyVersion = DeveloperPrivacyPolicyVersion,
                DeveloperCertificateBytes = DeveloperCertificateBytes,
                TenantUuid = TenantUuid,
                Name = TenantName,
                SubdomainPattern = SubdomainPattern,
                PrimaryColor = PrimaryColor,
                PrimaryColorHex = PrimaryColorHex,
                SupportEmail = SupportEmail,
                PermissionDeniedSupportMessage = new Dictionary<string, string>
                {
                    { PermissionDeniedSupportMessageCulture, PermissionDeniedSupportMessage }
                },
                Subscriptions = new List<SubscriptionModel>
                {
                    new SubscriptionModel
                    {
                        Uuid = SubscriptionUuid,
                        SubscriptionKeys = new[] { SubscriptionKey }
                    }
                },
                FeatureKeys = new List<string> { FeatureKey },
                OidcScopes = new[] { OidcScope },
                IsDraft = false,
                Version = TenantVersion,
                CreatedAt = CreatedAt
            };

            var serializerOptions = CreateSerializerOptions();

            var bytes = MessagePackSerializer.Serialize(tenantInfo, serializerOptions);

            var fixturePath = FixtureFile.GetPath(TenantInfoFixtureFileName);

            if (!File.Exists(fixturePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fixturePath));

                File.WriteAllBytes(fixturePath, bytes);

                Assert.Fail($"No fixture yet. Captured {fixturePath}, commit it only from the .NET 8 build.");
            }

            var fixtureBytes = File.ReadAllBytes(fixturePath);

            Assert.Equal(fixtureBytes, bytes);
        }

        // built like the options in RedisCacheImpl, from the formatter that AddTenants registers
        private static MessagePackSerializerOptions CreateSerializerOptions()
        {
            var compositeResolver = CompositeResolver.Create(
                formatters: new IMessagePackFormatter[]
                {
                    new TenantInfoMessagePackFormatterImpl()
                },
                resolvers: new IFormatterResolver[]
                {
                    ContractlessStandardResolverAllowPrivate.Instance
                });

            return ContractlessStandardResolverAllowPrivate.Options.WithResolver(compositeResolver);
        }

        [Fact]
        public void Net8TenantInfo_DeserializesToTheSameValues()
        {
            var fixturePath = FixtureFile.GetPath(TenantInfoFixtureFileName);

            var fixtureBytes = File.ReadAllBytes(fixturePath);

            var serializerOptions = CreateSerializerOptions();

            var tenantInfo = MessagePackSerializer.Deserialize<ITenantInfo>(fixtureBytes, serializerOptions);

            Assert.Equal(TenantUuid, tenantInfo.TenantUuid);
            Assert.Equal(TenantName, tenantInfo.Name);
            Assert.Equal(CreatedAt, tenantInfo.CreatedAt);
            Assert.Equal(PermissionDeniedSupportMessage, tenantInfo.PermissionDeniedSupportMessage[PermissionDeniedSupportMessageCulture]);
            Assert.Equal(SubscriptionUuid, tenantInfo.Subscriptions[0].Uuid);

            var bytes = MessagePackSerializer.Serialize(tenantInfo, serializerOptions);

            Assert.Equal(fixtureBytes, bytes);
        }
    }
}
