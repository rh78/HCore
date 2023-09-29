using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using HCore.Cache.Configuration;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace HCore.Cache.Impl
{
    internal class RedisCacheImpl : ICache, IDisposable
    {
        private const string _defaultHost = "127.0.0.1";
        private const string _defaultPort = "6379";

        private readonly ConnectionMultiplexer _connectionMultiplexer;
        private readonly IDatabase _database;

        private readonly BinaryFormatter _binaryFormatter = new();

        private MessagePackSerializerOptions _messagePackSerializerOptions;

        private bool _disposed;

        public RedisCacheImpl(IConfiguration configuration)
        {
            var redisConfiguration = configuration.GetSection("Cache:Redis").Get<RedisConfiguration>();

            var host = !string.IsNullOrEmpty(redisConfiguration.Host)
                ? redisConfiguration.Host
                : _defaultHost;

            var port = !string.IsNullOrEmpty(redisConfiguration.Port)
                ? redisConfiguration.Port
                : _defaultPort;

            _connectionMultiplexer = ConnectionMultiplexer.Connect($"{host}:{port}", (configurationOptions) =>
            {
                configurationOptions.ConnectRetry = redisConfiguration.ConnectRetry;
                configurationOptions.ConnectTimeout = redisConfiguration.ConnectTimeout;
            });

            _database = _connectionMultiplexer.GetDatabase();

            var formatResolver = CompositeResolver.Create(
                TypelessContractlessStandardResolver.Instance, 
                DynamicContractlessObjectResolver.Instance, 
                PrimitiveObjectResolver.Instance);

            _messagePackSerializerOptions = MessagePackSerializerOptions.Standard.WithResolver(formatResolver);
        }

        public async Task StoreAsync(string key, object value, TimeSpan expiresIn)
        {
            var bytes = Serialize(value);

            await _database.StringSetAsync(key, value: bytes, expiresIn, when: When.Always, flags: CommandFlags.None).ConfigureAwait(false);
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            var redisValue = await _database.StringGetAsync(key, flags: CommandFlags.None).ConfigureAwait(false);

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

            var redisValues = await _database.StringGetAsync(redisKeys, flags: CommandFlags.None).ConfigureAwait(false);

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
            await _database.KeyDeleteAsync(key, flags: CommandFlags.FireAndForget).ConfigureAwait(false);
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

        private byte[] Serialize(object value)
        {
            using var memoryStream = new MemoryStream();

#pragma warning disable SYSLIB0011 // Type or member is obsolete
            _binaryFormatter.Serialize(memoryStream, value);
#pragma warning disable SYSLIB0011 // Type or member is obsolete

            return memoryStream.ToArray();

            //var bytes = MessagePackSerializer.Typeless.Serialize(value, options: _messagePackSerializerOptions);

            //return bytes;
        }

        private T Deserialize<T>(byte[] bytes)
        {
            using var memoryStream = new MemoryStream(bytes);

#pragma warning disable SYSLIB0011 // Type or member is obsolete
            return (T)_binaryFormatter.Deserialize(memoryStream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete

            //var value = MessagePackSerializer.Typeless.Deserialize(bytes, options: _messagePackSerializerOptions);

            //return (T)value;
        }

        public void Dispose()
        {
            Dispose(disposing: true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (!disposing)
            {
                return;
            }

            _connectionMultiplexer.Dispose();

            _disposed = true;
        }
    }
}
