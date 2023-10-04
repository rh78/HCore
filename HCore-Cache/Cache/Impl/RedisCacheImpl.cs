#define DEBUG_SERIALIZATION

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions.Json;
using MessagePack;
using MessagePack.Resolvers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace HCore.Cache.Impl
{
    internal class RedisCacheImpl : ICache
    {
        private const int _db = 0;

        private static readonly MessagePackSerializerOptions _messagePackSerializerOptions = MessagePackSerializerOptions.Standard.WithResolver(TypelessObjectResolver.Instance);

        private readonly IRedisConnectionPool _redisConnectionPool;

        public RedisCacheImpl(IRedisConnectionPool redisConnectionPool)
        {
            _redisConnectionPool = redisConnectionPool;
        }

        private IConnectionMultiplexer ConnectionMultiplexer => _redisConnectionPool.GetConnectionMultiplexer();

        private IDatabase Database => ConnectionMultiplexer.GetDatabase(_db);

        public async Task StoreAsync(string key, object value, TimeSpan expiresIn)
        {
#if DEBUG_SERIALIZATION
            var expected = JToken.Parse(JsonConvert.SerializeObject(value));
#endif

            var bytes = Serialize(value);

            await Database.StringSetAsync(key, value: bytes, expiresIn, when: When.Always, flags: CommandFlags.None).ConfigureAwait(false);

#if DEBUG_SERIALIZATION
            if (expected != null)
            {
                var redisValue = await Database.StringGetAsync(key, flags: CommandFlags.None).ConfigureAwait(false);

                if (!redisValue.HasValue)
                {
                    throw new Exception("Redis value should be there, but is not");
                }

                var deserializedValue = MessagePackSerializer.Typeless.Deserialize(bytes, options: _messagePackSerializerOptions);

                var actual = JToken.Parse(JsonConvert.SerializeObject(deserializedValue));

                try
                {
                    actual.Should().BeEquivalentTo(expected);
                }
                catch (Exception e)
                { 
                    throw new Exception($"Value cached in Redis does not reproduce original value: {e}");
                }
            }
#endif
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            var redisValue = await Database.StringGetAsync(key, flags: CommandFlags.None).ConfigureAwait(false);

            if (redisValue.HasValue)
            {
                var result = Deserialize<T>(redisValue);

                return result;
            }

            return default;
        }

        public async Task<IDictionary<string, T>> GetAsync<T>(IEnumerable<string> keys) where T : class
        {
            if (keys == null)
            {
                return default;
            }

            var redisKeys = keys
                .Where(k => !string.IsNullOrEmpty(k))
                .Select(k => (RedisKey)k)
                .ToArray();

            var redisValues = await Database.StringGetAsync(redisKeys, flags: CommandFlags.None).ConfigureAwait(false);

            var valuesById = new Dictionary<string, T>(redisKeys.Length);

            for (int i = 0; i < redisValues.Length; i++)
            {
                RedisValue redisValue = redisValues[i];

                var key = redisKeys[i];

                var value = redisValue != RedisValue.Null
                    ? Deserialize<T>(redisValue)
                    : default;

                valuesById.Add(key, value);
            }

            return valuesById;
        }

        public async Task InvalidateAsync(string key)
        {
            await Database.KeyDeleteAsync(key, flags: CommandFlags.None).ConfigureAwait(false);
        }

        public void Store(string key, object value, TimeSpan expiresIn)
        {
            var bytes = Serialize(value);

            Database.StringSet(key, value: bytes, expiresIn, when: When.Always, flags: CommandFlags.None);
        }

        public T Get<T>(string key) where T : class
        {
            var redisValue = Database.StringGet(key, flags: CommandFlags.None);

            if (redisValue.HasValue)
            {
                var result = Deserialize<T>(redisValue);

                return result;
            }

            return default;
        }

        public Task<bool?> IsAvailableAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<bool?>(ConnectionMultiplexer.IsConnected);
        }

        private static byte[] Serialize(object value)
        {
            var bytes = MessagePackSerializer.Typeless.Serialize(value, options: _messagePackSerializerOptions);

            return bytes;
        }

        private static T Deserialize<T>(byte[] bytes)
        {
            var value = MessagePackSerializer.Typeless.Deserialize(bytes, options: _messagePackSerializerOptions);

            return (T)value;
        }
    }
}
