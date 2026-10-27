using System;
using HCore.Tenants.Models;
using HCore.Tenants.Models.Impl;
using HCore.Tenants.Options.Impl;
using HCore.Tenants.Providers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Xunit;

namespace HCore.Test
{
    // TenantOptionsCacheImpl on top of the OptionsCache of Microsoft.Extensions.Options, so a change of that base class shows here
    public class TenantOptionsCacheTests
    {
        private const string OptionsName = "Bearer";

        private const long DeveloperUuid = 1001;
        private const long TenantUuid = 2002;
        private const long SecondTenantUuid = 2003;
        private const int TenantVersion = 7;
        private const string AdditionalCacheKey = "devAdminSsoReplacement";

        private const string Authority = "https://auth.example.com";
        private const string SecondAuthority = "https://second-auth.example.com";

        [Fact]
        public void TwoTenants_GetSeparateOptions()
        {
            var tenantInfoAccessor = new TestTenantInfoAccessor();

            var optionsCache = new TenantOptionsCacheImpl<JwtBearerOptions>(tenantInfoAccessor);

            tenantInfoAccessor.TenantInfo = CreateTenantInfo(TenantUuid, TenantVersion, additionalCacheKey: null);

            var options = optionsCache.GetOrAdd(OptionsName, () => CreateOptions(Authority));

            tenantInfoAccessor.TenantInfo = CreateTenantInfo(SecondTenantUuid, TenantVersion, additionalCacheKey: null);

            var secondOptions = optionsCache.GetOrAdd(OptionsName, () => CreateOptions(SecondAuthority));

            Assert.Equal(Authority, options.Authority);
            Assert.Equal(SecondAuthority, secondOptions.Authority);
        }

        private static TenantInfoImpl CreateTenantInfo(long tenantUuid, int version, string additionalCacheKey)
        {
            return new TenantInfoImpl
            {
                DeveloperUuid = DeveloperUuid,
                TenantUuid = tenantUuid,
                Version = version,
                AdditionalCacheKey = additionalCacheKey
            };
        }

        private static JwtBearerOptions CreateOptions(string authority)
        {
            return new JwtBearerOptions
            {
                Authority = authority
            };
        }

        [Fact]
        public void SameTenant_GetsTheCachedInstance()
        {
            var tenantInfoAccessor = new TestTenantInfoAccessor
            {
                TenantInfo = CreateTenantInfo(TenantUuid, TenantVersion, additionalCacheKey: null)
            };

            var optionsCache = new TenantOptionsCacheImpl<JwtBearerOptions>(tenantInfoAccessor);

            var options = optionsCache.GetOrAdd(OptionsName, () => CreateOptions(Authority));
            var secondOptions = optionsCache.GetOrAdd(OptionsName, () => CreateOptions(SecondAuthority));

            Assert.Same(options, secondOptions);
        }

        [Fact]
        public void NewTenantVersion_GetsNewOptions()
        {
            var tenantInfoAccessor = new TestTenantInfoAccessor
            {
                TenantInfo = CreateTenantInfo(TenantUuid, TenantVersion, additionalCacheKey: null)
            };

            var optionsCache = new TenantOptionsCacheImpl<JwtBearerOptions>(tenantInfoAccessor);

            optionsCache.GetOrAdd(OptionsName, () => CreateOptions(Authority));

            tenantInfoAccessor.TenantInfo = CreateTenantInfo(TenantUuid, TenantVersion + 1, additionalCacheKey: null);

            var newOptions = optionsCache.GetOrAdd(OptionsName, () => CreateOptions(SecondAuthority));

            Assert.Equal(SecondAuthority, newOptions.Authority);
        }

        [Fact]
        public void AdditionalCacheKey_GetsSeparateOptions()
        {
            var tenantInfoAccessor = new TestTenantInfoAccessor
            {
                TenantInfo = CreateTenantInfo(TenantUuid, TenantVersion, additionalCacheKey: null)
            };

            var optionsCache = new TenantOptionsCacheImpl<JwtBearerOptions>(tenantInfoAccessor);

            optionsCache.GetOrAdd(OptionsName, () => CreateOptions(Authority));

            tenantInfoAccessor.TenantInfo = CreateTenantInfo(TenantUuid, TenantVersion, AdditionalCacheKey);

            var replacementOptions = optionsCache.GetOrAdd(OptionsName, () => CreateOptions(SecondAuthority));

            Assert.Equal(SecondAuthority, replacementOptions.Authority);
        }

        [Fact]
        public void NoTenant_CreatesOptionsOnEveryCall()
        {
            var tenantInfoAccessor = new TestTenantInfoAccessor();

            var optionsCache = new TenantOptionsCacheImpl<JwtBearerOptions>(tenantInfoAccessor);

            var options = optionsCache.GetOrAdd(OptionsName, () => CreateOptions(Authority));
            var secondOptions = optionsCache.GetOrAdd(OptionsName, () => CreateOptions(Authority));

            Assert.NotSame(options, secondOptions);
        }

        [Fact]
        public void TryRemove_DropsTheOptionsOfEveryTenant()
        {
            var tenantInfoAccessor = new TestTenantInfoAccessor
            {
                TenantInfo = CreateTenantInfo(TenantUuid, TenantVersion, additionalCacheKey: null)
            };

            var optionsCache = new TenantOptionsCacheImpl<JwtBearerOptions>(tenantInfoAccessor);

            optionsCache.GetOrAdd(OptionsName, () => CreateOptions(Authority));

            tenantInfoAccessor.TenantInfo = CreateTenantInfo(SecondTenantUuid, TenantVersion, additionalCacheKey: null);

            optionsCache.GetOrAdd(OptionsName, () => CreateOptions(Authority));

            var removed = optionsCache.TryRemove(OptionsName);

            var newOptions = optionsCache.GetOrAdd(OptionsName, () => CreateOptions(SecondAuthority));

            tenantInfoAccessor.TenantInfo = CreateTenantInfo(TenantUuid, TenantVersion, additionalCacheKey: null);

            var newFirstTenantOptions = optionsCache.GetOrAdd(OptionsName, () => CreateOptions(SecondAuthority));

            Assert.True(removed);
            Assert.Equal(SecondAuthority, newOptions.Authority);
            Assert.Equal(SecondAuthority, newFirstTenantOptions.Authority);
        }

        [Fact]
        public void TryAdd_IsReturnedByGetOrAdd()
        {
            var tenantInfoAccessor = new TestTenantInfoAccessor
            {
                TenantInfo = CreateTenantInfo(TenantUuid, TenantVersion, additionalCacheKey: null)
            };

            var optionsCache = new TenantOptionsCacheImpl<JwtBearerOptions>(tenantInfoAccessor);

            var addedOptions = CreateOptions(Authority);

            var added = optionsCache.TryAdd(OptionsName, addedOptions);

            var options = optionsCache.GetOrAdd(OptionsName, () => CreateOptions(SecondAuthority));

            Assert.True(added);
            Assert.Same(addedOptions, options);
        }

        [Fact]
        public void GetOrAdd_WithoutFactory_Throws()
        {
            var tenantInfoAccessor = new TestTenantInfoAccessor();

            var optionsCache = new TenantOptionsCacheImpl<JwtBearerOptions>(tenantInfoAccessor);

            Assert.Throws<ArgumentNullException>(() => optionsCache.GetOrAdd(OptionsName, null));
        }

        private class TestTenantInfoAccessor : ITenantInfoAccessor
        {
            public ITenantInfo TenantInfo { get; set; }

            public string MatchedSubDomain { get; set; }
        }
    }
}
