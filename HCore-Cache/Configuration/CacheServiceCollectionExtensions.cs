using Microsoft.Extensions.Configuration;
using HCore.Cache;
using HCore.Cache.Impl;
using System;

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
                string connectionString = configuration["Cache:Redis:ConnectionString"];

                if (string.IsNullOrEmpty(connectionString))
                    throw new Exception("Redis cache connection string is empty");

                string instanceName = configuration["Cache:Redis:InstanceName"];

                if (string.IsNullOrEmpty(instanceName))
                    throw new Exception("Redis instance name is empty");

                services.AddDistributedRedisCache(options =>
                {
                    options.Configuration = connectionString;
                    options.InstanceName = instanceName;
                });

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
