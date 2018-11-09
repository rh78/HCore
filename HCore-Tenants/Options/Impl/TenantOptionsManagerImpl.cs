using Microsoft.Extensions.Options;

// based on https://github.com/Finbuckle/Finbuckle.MultiTenant/blob/master/src/Finbuckle.MultiTenant.AspNetCore/Internal/MultiTenantOptionsManager.cs

namespace HCore.Tenants.Options.Impl
{
    internal class TenantOptionsManagerImpl<TOptions> : IOptions<TOptions>, IOptionsSnapshot<TOptions> where TOptions : class, new()
    {
        private readonly IOptionsFactory<TOptions> _factory;
        private readonly IOptionsMonitorCache<TOptions> _cache; // Note: this is a private cache

        public TenantOptionsManagerImpl(IOptionsFactory<TOptions> factory, IOptionsMonitorCache<TOptions> cache)
        {
            _factory = factory;
            _cache = cache;
        }

        public TOptions Value
        {
            get
            {
                return Get(Microsoft.Extensions.Options.Options.DefaultName);
            }
        }

        public virtual TOptions Get(string name)
        {
            name = name ?? Microsoft.Extensions.Options.Options.DefaultName;

            // Store the options in our instance cache

            return _cache.GetOrAdd(name, () => _factory.Create(name));
        }
    }
}
