using System;
using HCore.Cache;
using HCore.Cache.Impl;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CacheServiceCollectionExtensions
    {
        public static IServiceCollection AddCache(this IServiceCollection services, IConfiguration configuration)
        {
            Console.WriteLine("Initializing cache...");

            string implementation = configuration["Cache:Implementation"];

            if (string.IsNullOrEmpty(implementation))
                throw new Exception("Cache implementation specification is empty");

            if (!implementation.Equals(CacheConstants.CacheImplementationRedis))
                throw new Exception("Cache implementation specification is invalid");

            if (implementation.Equals(CacheConstants.CacheImplementationRedis))
            {
                services.AddSingleton<IRedisConnectionPool, RedisConnectionPoolImpl>();
                services.AddSingleton<ICache, RedisCacheImpl>();
            }

            Console.WriteLine("Cache initialized successfully");

            return services;
        }
    }
}
