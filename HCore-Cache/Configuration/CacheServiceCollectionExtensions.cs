using System;
using HCore.Cache;
using HCore.Cache.Impl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

            if (!implementation.Equals(CacheConstants.CacheImplementationRedis) && !implementation.Equals(CacheConstants.CacheImplementationMemcached))
                throw new Exception("Cache implementation specification is invalid");

            if (implementation.Equals(CacheConstants.CacheImplementationRedis))
            {
                services.AddSingleton<ICache, RedisCacheImpl>();
            }
            else
            {
                services.AddMemcached(options =>
                {
                    configuration.GetSection("Cache:Memcached").Bind(options);

                    options.Protocol = Enyim.Caching.Memcached.MemcachedProtocol.Binary;
                });

                services.AddSingleton<ICache, MemcachedCacheImpl>();
            }

            Console.WriteLine("Cache initialized successfully");

            return services;
        }
    }
}
