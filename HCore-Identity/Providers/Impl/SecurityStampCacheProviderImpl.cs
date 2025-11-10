using System;
using System.Threading.Tasks;
using HCore.Cache;

namespace HCore.Identity.Providers.Impl
{
    public class SecurityStampCacheProviderImpl : ISecurityStampCacheProvider
    {
        private readonly ICache _cache;

        public SecurityStampCacheProviderImpl(ICache cache)
        {
            _cache = cache;
        }

        public async Task<string> GetSecurityStampAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            var cacheKey = GetCacheKey(userId);

            return await _cache.GetStringAsync($"icss_{userId}").ConfigureAwait(false);
        }

        public async Task CreateOrUpdateSecurityStampAsync(string userId, string securityStamp)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            var cacheKey = GetCacheKey(userId);

            await _cache.StoreAsync(cacheKey, securityStamp, TimeSpan.FromMinutes(30)).ConfigureAwait(false);
        }

        public async Task InvalidateSecurityStampAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            var cacheKey = GetCacheKey(userId);

            await _cache.InvalidateAsync(cacheKey).ConfigureAwait(false);
        }

        private string GetCacheKey(string userId)
        {
            return $"icss_{userId}";
        }
    }
}
