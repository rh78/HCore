using HCore.Tenants.Providers;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

// based on https://github.com/Finbuckle/Finbuckle.MultiTenant/blob/master/src/Finbuckle.MultiTenant.AspNetCore/Internal/MultiTenantOptionsCache.cs

namespace HCore.Tenants.Options.Impl
{
    internal class TenantOptionsCacheImpl<TOptions> : OptionsCache<TOptions> where TOptions : class
    {
        private readonly ITenantInfoAccessor _tenantInfoAccessor;
        
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _adjustedOptionsNames =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, object>>();

        public TenantOptionsCacheImpl(ITenantInfoAccessor tenantInfoAccessor)
        {
            _tenantInfoAccessor = tenantInfoAccessor;
        }

        public override TOptions GetOrAdd(string name, Func<TOptions> createOptions)
        {
            if (createOptions == null)
            {
                throw new ArgumentNullException(nameof(createOptions));
            }

            name = name ?? Microsoft.Extensions.Options.Options.DefaultName;

            var adjustedOptionsName = AdjustOptionsName(_tenantInfoAccessor.TenantInfo.DeveloperUuid, _tenantInfoAccessor.TenantInfo.TenantUuid, _tenantInfoAccessor.TenantInfo.AdditionalCacheKey, _tenantInfoAccessor.TenantInfo.Version, name);

            return base.GetOrAdd(adjustedOptionsName, () => TenantsFactoryWrapper(name, adjustedOptionsName, createOptions));
        }

        public override bool TryAdd(string name, TOptions options)
        {
            name = name ?? Microsoft.Extensions.Options.Options.DefaultName;

            var adjustedOptionsName = AdjustOptionsName(_tenantInfoAccessor.TenantInfo.DeveloperUuid, _tenantInfoAccessor.TenantInfo.TenantUuid, _tenantInfoAccessor.TenantInfo.AdditionalCacheKey, _tenantInfoAccessor.TenantInfo.Version, name);

            if (base.TryAdd(adjustedOptionsName, options))
            {
                CacheAdjustedOptionsName(name, adjustedOptionsName);

                return true;
            }

            return false;
        }

        public override bool TryRemove(string name)
        {
            name = name ?? Microsoft.Extensions.Options.Options.DefaultName;
            var result = false;

            if (!_adjustedOptionsNames.TryGetValue(name, out var adjustedOptionsNames))
                return false;

            List<string> removedNames = new List<string>();

            foreach (var adjustedOptionsName in adjustedOptionsNames)
            {
                if (base.TryRemove(adjustedOptionsName.Key))
                {
                    removedNames.Add(adjustedOptionsName.Key);
                    result = true;
                }
            }

            foreach (var removedName in removedNames)
            {
                adjustedOptionsNames.TryRemove(removedName, out var dummy);
            }

            return result;
        }

        private string AdjustOptionsName(long? developerUuid, long? tenantUuid, string additionalCacheKey, long version, string name)
        {
            if (developerUuid == null)
                throw new Exception("Developer UUID is empty");

            if (tenantUuid == null)
                throw new Exception("Tenant UUID is empty");

            // Hash so that prefix + option name can't cause a collision. 

            string key = $"{developerUuid}:{tenantUuid}";

            if (!string.IsNullOrEmpty(additionalCacheKey))
                key = $"{developerUuid}:{tenantUuid}:{additionalCacheKey}:{version}";

            byte[] buffer = Encoding.UTF8.GetBytes(key);

            var sha1 = System.Security.Cryptography.SHA1.Create();
            var hash = sha1.ComputeHash(buffer);

            string prefix = Convert.ToBase64String(hash);

            return prefix + name;
        }

        private TOptions TenantsFactoryWrapper(string optionsName, string adjustedOptionsName, Func<TOptions> createOptions)
        {
            if (createOptions == null)
            {
                throw new ArgumentNullException(nameof(createOptions));
            }

            var options = createOptions();

            CacheAdjustedOptionsName(optionsName, adjustedOptionsName);

            return options;
        }

        private void CacheAdjustedOptionsName(string optionsName, string adjustedOptionsName)
        {
            _adjustedOptionsNames.GetOrAdd(optionsName, new ConcurrentDictionary<string, object>()).TryAdd(adjustedOptionsName, null);
        }
    }
}
