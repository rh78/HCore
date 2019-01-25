using Microsoft.Extensions.Caching.Distributed;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace HCore.Cache.Impl
{
    internal class RedisCacheImpl : ICache
    {
        private readonly IDistributedCache _distributedCache;

        public RedisCacheImpl(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        public async Task StoreAsync(string key, object value, TimeSpan? expiresIn = null)
        {
            await _distributedCache.SetAsync(key, ToByteArray(value), new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = expiresIn != null ? DateTimeOffset.Now.Add((TimeSpan)expiresIn) : (DateTimeOffset?)null
            }).ConfigureAwait(false);
        }
        
        public async Task<T> GetAsync<T>(string key) where T : class
        {
            byte[] value = await _distributedCache.GetAsync(key).ConfigureAwait(false);

            if (value == null)
                return null;

            return FromByteArray<T>(value);
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
                binaryFormatter.Serialize(memoryStream, value);
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
                return binaryFormatter.Deserialize(memoryStream) as T;
            }
        }
    }
}
