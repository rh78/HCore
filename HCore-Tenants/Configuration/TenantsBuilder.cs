using HCore.Tenants;
using HCore.Tenants.Impl;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;

// based on https://github.com/Finbuckle/Finbuckle.MultiTenant/blob/master/src/Finbuckle.MultiTenant.AspNetCore/MultiTenantBuilder.cs

namespace Microsoft.Extensions.DependencyInjection
{
    public class TenantsBuilder
    {
        internal readonly IServiceCollection services;

        public TenantsBuilder(IServiceCollection services)
        {
            this.services = services;
        }

        public TenantsBuilder WithPerTenantOptions<TOptions>(Action<TOptions, ITenantInfo> tenantInfo) where TOptions : class, new()
        {
            if (tenantInfo == null)
            {
                throw new ArgumentNullException(nameof(tenantInfo));
            }

            // Handles multiplexing cached options.

            services.TryAddSingleton<IOptionsMonitorCache<TOptions>>(sp =>
            {
                return (TenantOptionsCacheImpl<TOptions>)
                    ActivatorUtilities.CreateInstance(sp, typeof(TenantOptionsCacheImpl<TOptions>));
            });

            // Necessary to apply tenant options in between configuration and postconfiguration
            services.TryAddTransient<IOptionsFactory<TOptions>>(sp =>
            {
                return (IOptionsFactory<TOptions>)ActivatorUtilities.
                    CreateInstance(sp, typeof(TenantOptionsFactoryImpl<TOptions>), new[] { tenantInfo });
            });

            services.TryAddScoped<IOptionsSnapshot<TOptions>>(sp => BuildOptionsManager<TOptions>(sp));

            services.TryAddSingleton<IOptions<TOptions>>(sp => BuildOptionsManager<TOptions>(sp));

            return this;
        }

        private static TenantOptionsManagerImpl<TOptions> BuildOptionsManager<TOptions>(IServiceProvider sp) where TOptions : class, new()
        {
            var cache = ActivatorUtilities.CreateInstance(sp, typeof(TenantOptionsCacheImpl<TOptions>));
            return (TenantOptionsManagerImpl<TOptions>)
                ActivatorUtilities.CreateInstance(sp, typeof(TenantOptionsManagerImpl<TOptions>), new[] { cache });
        }        
    }
}