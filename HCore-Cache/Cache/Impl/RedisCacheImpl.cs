﻿// #define DEBUG_SERIALIZATION
// #define BINARY_FORMATTER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

#if DEBUG_SERIALIZATION
using FluentAssertions.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

#if BINARY_FORMATTER
using System.Runtime.Serialization.Formatters.Binary;
using CacheUtils;
#endif

namespace HCore.Cache.Impl
{
    internal class RedisCacheImpl : ICache
    {
        private readonly MessagePackSerializerOptions _messagePackSerializerOptions;

        private const int _db = 0;

        private readonly IRedisConnectionPool _redisConnectionPool;

        public RedisCacheImpl(IRedisConnectionPool redisConnectionPool, IServiceProvider serviceProvider)
        {
            _redisConnectionPool = redisConnectionPool;

            var messagePackFormatters = serviceProvider.GetServices<IMessagePackFormatter>()?.ToArray();

            var compositeResolver = CompositeResolver.Create(
                formatters: messagePackFormatters,
                resolvers: new IFormatterResolver[]
                {
                    ContractlessStandardResolverAllowPrivate.Instance
                });

            _messagePackSerializerOptions = ContractlessStandardResolverAllowPrivate.Options.WithResolver(compositeResolver);
        }

        private IConnectionMultiplexer ConnectionMultiplexer => _redisConnectionPool.GetConnectionMultiplexer();

        private IDatabase Database => ConnectionMultiplexer.GetDatabase(_db);

        public async Task StoreAsync<T>(string key, T value, TimeSpan expiresIn)
        {
#if DEBUG_SERIALIZATION
            JToken expected = null;

            try
            {
                expected = JToken.Parse(JsonConvert.SerializeObject(value));
            }
            catch (Exception)
            {
                // ignore
            }
#endif
            var bytes = Serialize(value);

            if (bytes == null)
            {
                return;
            }

            await ExecuteWithFallbackAsync(() => Database.StringSetAsync(key, value: bytes, expiresIn, when: When.Always, flags: CommandFlags.None)).ConfigureAwait(false);

#if DEBUG_SERIALIZATION
            if (expected != null)
            {
                var redisValue = await ExecuteWithFallbackAsync(() => Database.StringGetAsync(key, flags: CommandFlags.None)).ConfigureAwait(false);

                if (!redisValue.HasValue)
                {
                    throw new Exception("Redis value should be there, but is not");
                }

                var typeCode = value == null
                    ? TypeCode.DBNull
                    : Type.GetTypeCode(value.GetType());

                object deserializedValue = typeCode == TypeCode.String
                    ? DeserializeString(bytes)
                    : DeserializeObject<T>(bytes);

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

        public async Task<string> GetStringAsync(string key)
        {
            var redisValue = await ExecuteWithFallbackAsync(() => Database.StringGetAsync(key, flags: CommandFlags.None)).ConfigureAwait(false);

            if (redisValue.HasValue)
            {
                var result = DeserializeString(redisValue);

                return result;
            }

            return default;
        }

        public async Task<T> GetObjectAsync<T>(string key) where T : class
        {
            var redisValue = await ExecuteWithFallbackAsync(() => Database.StringGetAsync(key, flags: CommandFlags.None)).ConfigureAwait(false);

            if (redisValue.HasValue)
            {
                var result = DeserializeObject<T>(redisValue);

                return result;
            }

            return default;
        }

        public async Task<IDictionary<string, string>> GetStringsAsync(IEnumerable<string> keys)
        {
            if (keys == null)
            {
                return default;
            }

            var redisKeys = keys
                .Where(k => !string.IsNullOrEmpty(k))
                .Select(k => (RedisKey)k)
                .ToArray();

            var redisValues = await ExecuteWithFallbackAsync(() => Database.StringGetAsync(redisKeys, flags: CommandFlags.None)).ConfigureAwait(false);

            var valuesById = new Dictionary<string, string>(redisKeys.Length);

            for (int i = 0; i < redisValues.Length; i++)
            {
                RedisValue redisValue = redisValues[i];

                var key = redisKeys[i];

                var value = redisValue != RedisValue.Null
                    ? DeserializeString(redisValue)
                    : default;

                valuesById.Add(key, value);
            }

            return valuesById;
        }

        public async Task<IDictionary<string, T>> GetObjectsAsync<T>(IEnumerable<string> keys) where T : class
        {
            if (keys == null)
            {
                return default;
            }

            var redisKeys = keys
                .Where(k => !string.IsNullOrEmpty(k))
                .Select(k => (RedisKey)k)
                .ToArray();

            var redisValues = await ExecuteWithFallbackAsync(() => Database.StringGetAsync(redisKeys, flags: CommandFlags.None)).ConfigureAwait(false);

            var valuesById = new Dictionary<string, T>(redisKeys.Length);

            for (int i = 0; i < redisValues.Length; i++)
            {
                RedisValue redisValue = redisValues[i];

                var key = redisKeys[i];

                var value = redisValue != RedisValue.Null
                    ? DeserializeObject<T>(redisValue)
                    : default;

                valuesById.Add(key, value);
            }

            return valuesById;
        }

        public async Task InvalidateAsync(string key)
        {
            await ExecuteWithFallbackAsync(() => Database.KeyDeleteAsync(key, flags: CommandFlags.None)).ConfigureAwait(false);
        }

        public Task<bool?> IsAvailableAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<bool?>(ConnectionMultiplexer.IsConnected);
        }

        private async Task<TOut> ExecuteWithFallbackAsync<TOut>(Func<Task<TOut>> func)
        {
            var tryCount = 1;

            do
            {
                try
                {
                    return await func().ConfigureAwait(false);
                }
                catch (RedisConnectionException)                
                {
                    // timeout, or something?

                    if (tryCount >= 3)
                    {
                        throw;
                    }
                    
                    tryCount++;

                    await Task.Delay(1000).ConfigureAwait(false);
                }
                catch (RedisTimeoutException)
                {
                    // timeout

                    if (tryCount >= 3)
                    {
                        throw;
                    }

                    tryCount++;

                    await Task.Delay(1000).ConfigureAwait(false);
                }
                catch (RedisServerException)
                {
                    // cluster down?

                    if (tryCount >= 3)
                    {
                        throw;
                    }

                    tryCount++;

                    await Task.Delay(1000).ConfigureAwait(false);
                }
            }
            while (true);
        }

        private byte[] Serialize<T>(T value)
        {
            byte[] bytes = null;

#if BINARY_FORMATTER
            var typeCode = value == null ? TypeCode.DBNull : Type.GetTypeCode(value.GetType());

            switch (typeCode)
            {
                case TypeCode.Empty:
                case TypeCode.DBNull:
                    return null;

                case TypeCode.String:
                    bytes = Encoding.UTF8.GetBytes(value as string);
                    break;

                default:
                    if (value.GetType().IsSerializable)
                    {
                        using (var stream = Helper.CreateMemoryStream())
                        {
#pragma warning disable SYSLIB0011 // Typ oder Element ist veraltet
                            new BinaryFormatter().Serialize(stream, value);
#pragma warning restore SYSLIB0011 // Typ oder Element ist veraltet
                            bytes = stream.ToBytes();
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"The type '{value.GetType()}' of '{nameof(value)}' must have Serializable attribute");
                    }

                    break;
            }
#else
            if (value is string stringValue)
            {
                bytes = Encoding.UTF8.GetBytes(stringValue);
            }
            else
            {
                bytes = MessagePackSerializer.Serialize(value, _messagePackSerializerOptions);
            }
#endif

            return bytes;
        }

        private static string DeserializeString(byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }

            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        private T DeserializeObject<T>(byte[] bytes)
        {
            if (bytes == null)
            {
                return default;
            }

            T value;

#if BINARY_FORMATTER
            using (var stream = Helper.CreateMemoryStream(bytes, 0, bytes.Length))
            {
#pragma warning disable SYSLIB0011 // Typ oder Element ist veraltet
                value = (T)new BinaryFormatter().Deserialize(stream);
#pragma warning restore SYSLIB0011 // Typ oder Element ist veraltet
            }
#else
            value = MessagePackSerializer.Deserialize<T>(bytes, _messagePackSerializerOptions);
#endif

            return value;
        }
    }
}
