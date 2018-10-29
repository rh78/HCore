using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

// based on https://github.com/Finbuckle/Finbuckle.MultiTenant/blob/master/src/Finbuckle.MultiTenant.AspNetCore/Internal/MultiTenantOptionsFactory.cs

namespace HCore.Tenants.Impl
{
    internal class TenantOptionsFactoryImpl<TOptions> : IOptionsFactory<TOptions> where TOptions : class, new()
    {
        private readonly IEnumerable<IConfigureOptions<TOptions>> _setups;
        private readonly Action<TOptions, ITenantInfo> _tenantConfig;
        private readonly ITenantInfoAccessor _tenantInfoAccessor;
        private readonly IEnumerable<IPostConfigureOptions<TOptions>> _postConfigures;

        public TenantOptionsFactoryImpl(IEnumerable<IConfigureOptions<TOptions>> setups, IEnumerable<IPostConfigureOptions<TOptions>> postConfigures, Action<TOptions, ITenantInfo> tenantConfig, ITenantInfoAccessor tenantInfoAccessor)
        {
            _setups = setups;

            _tenantConfig = tenantConfig;
            _tenantInfoAccessor = tenantInfoAccessor;

            _postConfigures = postConfigures;
        }

        public TOptions Create(string name)
        {
            var options = new TOptions();
            foreach (var setup in _setups)
            {
                if (setup is IConfigureNamedOptions<TOptions> namedSetup)
                {
                    namedSetup.Configure(name, options);
                }
                else if (name == Options.DefaultName)
                {
                    setup.Configure(options);
                }
            }
            
            if (_tenantInfoAccessor.TenantInfo != null)
            {
                _tenantConfig(options, _tenantInfoAccessor.TenantInfo);
            }

            foreach (var post in _postConfigures)
            {
                post.PostConfigure(name, options);
            }

            return options;
        }
    }
}
