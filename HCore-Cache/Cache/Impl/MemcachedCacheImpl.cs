using Enyim.Caching;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HCore.Cache.Impl
{
    internal class MemcachedCacheImpl : ICache
    {
        private readonly IMemcachedClient _memcachedClient;

        private readonly ILogger<MemcachedCacheImpl> _logger;

        // be careful, do not use Memcached 1.4.4 for Windows, it has wrong server time!
        // download 1.4.5!
        // https://blog.elijaa.org/2010/10/15/memcached-for-windows/

        public MemcachedCacheImpl(IMemcachedClient memcachedClient, ILogger<MemcachedCacheImpl> logger)
        {
            _memcachedClient = memcachedClient;

            _logger = logger;
        }

        public void Store(string key, object value, TimeSpan expiresIn)
        {
            int cacheMinutes = (int)expiresIn.TotalMinutes;

            bool successfullyWritten = _memcachedClient.Set(key, value, cacheMinutes);

            if (!successfullyWritten)
            {
                _logger.LogWarning($"Value for key {key} could not be written to Memcached cache");
            }
        }

        public async Task StoreAsync(string key, object value, TimeSpan expiresIn)
        {
            int cacheMinutes = (int)expiresIn.TotalMinutes;

            bool successfullyWritten = await _memcachedClient.SetAsync(key, value, cacheMinutes).ConfigureAwait(false);

            if (!successfullyWritten)
            {
                _logger.LogWarning($"Value for key {key} could not be written to Memcached cache");
            }
        }

        public T Get<T>(string key) where T : class
        {
            return _memcachedClient.Get<T>(key);
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            return await _memcachedClient.GetAsync<T>(key).ConfigureAwait(false);
        }

        public async Task InvalidateAsync(string key)
        {
            bool successfullyRemoved = await _memcachedClient.RemoveAsync(key).ConfigureAwait(false);

            if (!successfullyRemoved)
            {
                _logger.LogWarning($"Value for key {key} could not be removed from Memcached cache");
            }
        }
    }
}
