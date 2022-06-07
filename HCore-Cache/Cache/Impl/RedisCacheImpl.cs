using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace HCore.Cache.Impl
{
    internal class RedisCacheImpl : ICache
    {
        private readonly IRedisDatabase _redisDatabase;

        public RedisCacheImpl(IRedisDatabase redisDatabase)
        {
            _redisDatabase = redisDatabase;
        }

        public async Task StoreAsync(string key, object value, TimeSpan expiresIn)
        {
            await _redisDatabase.AddAsync(key, value, When.Always, CommandFlags.None).ConfigureAwait(false);
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            return await _redisDatabase.GetAsync<T>(key, CommandFlags.None).ConfigureAwait(false);
        }

        public async Task<IDictionary<string, T>> GetAsync<T>(IEnumerable<string> keys) where T : class
        {
            return await _redisDatabase.GetAllAsync<T>(keys).ConfigureAwait(false);
        }

        public async Task InvalidateAsync(string key)
        {
            await _redisDatabase.RemoveAsync(key, CommandFlags.None).ConfigureAwait(false);
        }

        public void Store(string key, object value, TimeSpan expiresIn)
        {
            throw new NotImplementedException();
        }

        public T Get<T>(string key) where T : class
        {
            throw new NotImplementedException();
        }

        public Task<bool?> IsAvailableAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<bool?>(true);
        }
    }
}
