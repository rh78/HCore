using Enyim.Caching;
using System;
using System.Threading.Tasks;

namespace HCore.Cache.Impl
{
    internal class MemcachedCacheImpl : ICache
    {
        private readonly IMemcachedClient _memcachedClient;

        // be careful, do not use Memcached 1.4.4 for Windows, it has wrong server time!
        // download 1.4.5!
        // https://blog.elijaa.org/2010/10/15/memcached-for-windows/

        public MemcachedCacheImpl(IMemcachedClient memcachedClient)
        {
            _memcachedClient = memcachedClient;
        }

        public async Task StoreAsync(string key, object value, TimeSpan expiresIn)
        {
            int cacheMinutes = (int)expiresIn.TotalMinutes;

            await _memcachedClient.SetAsync(key, value, cacheMinutes).ConfigureAwait(false);
        }
        
        public async Task<T> GetAsync<T>(string key) where T : class
        {
            return await _memcachedClient.GetAsync<T>(key).ConfigureAwait(false);
        }

        public async Task InvalidateAsync(string key)
        {
            await _memcachedClient.RemoveAsync(key).ConfigureAwait(false);
        }
    }
}
