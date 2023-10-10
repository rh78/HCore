using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Enyim.Caching;
using Microsoft.Extensions.Logging;

namespace HCore.Cache.Impl
{
    internal class MemcachedCacheImpl : ICache
    {
        private const byte CacheKeyMaxBytes = 250;

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

        public async Task StoreAsync(string key, object value, TimeSpan expiresIn)
        {
            key = EnsureKeyMaxLengthNotReached(key);

            int cacheMinutes = (int)expiresIn.TotalMinutes;

            bool successfullyWritten = await _memcachedClient.SetAsync(key, value, cacheMinutes).ConfigureAwait(false);

            if (!successfullyWritten)
            {
                _logger.LogWarning($"Value for key {key} could not be written to Memcached cache");
            }
        }

        public async Task<string> GetStringAsync(string key)
        {
            key = EnsureKeyMaxLengthNotReached(key);

            return await _memcachedClient.GetAsync<string>(key).ConfigureAwait(false);
        }

        public async Task<T> GetObjectAsync<T>(string key) where T : class
        {
            key = EnsureKeyMaxLengthNotReached(key);

            return await _memcachedClient.GetAsync<T>(key).ConfigureAwait(false);
        }

        public async Task<IDictionary<string, string>> GetStringsAsync(IEnumerable<string> keys)
        {
            keys = EnsureKeyMaxLengthNotReached(keys);

            return await _memcachedClient.GetAsync<string>(keys).ConfigureAwait(false);
        }

        public async Task<IDictionary<string, T>> GetObjectsAsync<T>(IEnumerable<string> keys) where T : class
        {
            keys = EnsureKeyMaxLengthNotReached(keys);

            return await _memcachedClient.GetAsync<T>(keys).ConfigureAwait(false);
        }

        public async Task InvalidateAsync(string key)
        {
            key = EnsureKeyMaxLengthNotReached(key);

            bool successfullyRemoved = await _memcachedClient.RemoveAsync(key).ConfigureAwait(false);

            if (!successfullyRemoved)
            {
                _logger.LogInformation($"Value for key {key} could not be removed from Memcached cache");
            }
        }

        private static IEnumerable<string> EnsureKeyMaxLengthNotReached(IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                var bytesCount = Encoding.UTF8.GetByteCount(key.AsSpan());

                if (bytesCount > CacheKeyMaxBytes)
                {
                    var encodedKey = EncodeKey(key);

                    yield return encodedKey;
                }

                yield return key;
            }
        }

        private static string EnsureKeyMaxLengthNotReached(string key)
        {
            var bytesCount = Encoding.UTF8.GetByteCount(key.AsSpan());

            if (bytesCount > CacheKeyMaxBytes)
            {
                var encodedKey = EncodeKey(key);

                return encodedKey;
            }

            return key;
        }

        private static string EncodeKey(string key)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(key);
                var challengeBytes = md5.ComputeHash(bytes);

                var encodedKey = Convert.ToBase64String(challengeBytes);

                return encodedKey;
            }
        }

        public async Task<bool?> IsAvailableAsync(CancellationToken cancellationToken)
        {
            try
            {
                var stats = await _memcachedClient.StatsAsync(cancellationToken).ConfigureAwait(false);

                return stats != null;
            }
            catch (NullReferenceException)
            {
                return false;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }
    }
}
