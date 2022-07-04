using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace HCore.Cache.Impl
{
    internal class RedisCacheImpl : ICache
    {
        private readonly IDistributedCache _distributedCache;

        public RedisCacheImpl(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        public void Store(string key, object value, TimeSpan expiresIn)
        {
            _distributedCache.Set(key, ToByteArray(value), new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = DateTimeOffset.Now.Add(expiresIn)
            });
        }

        public async Task StoreAsync(string key, object value, TimeSpan expiresIn)
        {
            await _distributedCache.SetAsync(key, ToByteArray(value), new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = DateTimeOffset.Now.Add(expiresIn)
            }).ConfigureAwait(false);
        }

        public T Get<T>(string key) where T : class
        {
            byte[] value = _distributedCache.Get(key);

            if (value == null)
                return null;

            return FromByteArray<T>(value);
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            byte[] value = await _distributedCache.GetAsync(key).ConfigureAwait(false);

            if (value == null)
                return null;

            return FromByteArray<T>(value);
        }

        public Task<IDictionary<string, T>> GetAsync<T>(IEnumerable<string> keys) where T : class
        {
            throw new NotImplementedException();
        }

        public async Task InvalidateAsync(string key)
        {
            await _distributedCache.RemoveAsync(key).ConfigureAwait(false);
        }

        private byte[] ToByteArray<T>(T value)
        {
            if (value == null)
            {
                return null;
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
#pragma warning disable SYSLIB0011 // Typ oder Element ist veraltet
                binaryFormatter.Serialize(memoryStream, value);
#pragma warning restore SYSLIB0011 // Typ oder Element ist veraltet
                return memoryStream.ToArray();
            }
        }

        private T FromByteArray<T>(byte[] byteArray) where T : class
        {
            if (byteArray == null)
            {
                return default(T);
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();

            using (MemoryStream memoryStream = new MemoryStream(byteArray))
            {
#pragma warning disable SYSLIB0011 // Typ oder Element ist veraltet
                return binaryFormatter.Deserialize(memoryStream) as T;
#pragma warning restore SYSLIB0011 // Typ oder Element ist veraltet
            }
        }
    }
}
