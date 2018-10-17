using Microsoft.Extensions.Configuration;
using HCore.Redis;
using HCore.Redis.Impl;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RedisServiceCollectionExtensions
    {
        public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
        {
            Console.WriteLine("Initializing Redis distributed cache...");

            string connectionString = configuration["Redis:ConnectionString"];

            if (string.IsNullOrEmpty(connectionString))
                throw new Exception("Redis connection string is empty");

            string instanceName = configuration["Redis:InstanceName"];

            if (string.IsNullOrEmpty(instanceName))
                throw new Exception("Redis instance name is empty");

            services.AddDistributedRedisCache(options =>
            {
                options.Configuration = connectionString;
                options.InstanceName = instanceName;
            });

            services.AddSingleton<IRedisCache, RedisCacheImpl>();

            Console.WriteLine("Redis distributed cache initialized successfully");

            return services;
        }
    }
}
