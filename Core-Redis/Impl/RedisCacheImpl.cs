using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ReinhardHolzner.Core.Redis.Impl
{
    internal class RedisCacheImpl : IRedisCache
    {
        private readonly IDistributedCache _distributedCache;

        public RedisCacheImpl(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        public async Task StoreAsync(string key, int[] values)
        {
            await _distributedCache.SetAsync(key, ToByteArray(values.ToList())).ConfigureAwait(false);
        }

        public async Task StoreAsync(string key, long[] values)
        {
            await _distributedCache.SetAsync(key, ToByteArray(values.ToList())).ConfigureAwait(false);
        }

        public async Task<int[]> GetIntArrayAsync(string key)
        {
            byte[] value = await _distributedCache.GetAsync(key).ConfigureAwait(false);

            if (value == null)
                return null;

            return FromByteArray<List<int>>(value).ToArray();
        }

        public async Task<long[]> GetLongArrayAsync(string key)
        {
            byte[] value = await _distributedCache.GetAsync(key).ConfigureAwait(false);

            if (value == null)
                return null;

            return FromByteArray<List<long>>(value).ToArray();
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
