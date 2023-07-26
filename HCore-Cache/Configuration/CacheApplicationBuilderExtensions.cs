using HCore.Cache;
using Microsoft.Extensions.Configuration;
using System;

namespace Microsoft.AspNetCore.Builder
{
    public static class CacheApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseCache(this IApplicationBuilder app, IConfiguration configuration)
        {
            string implementation = configuration[$"Cache:Implementation"];

            if (string.IsNullOrEmpty(implementation))
                throw new Exception("Cache implementation specification is empty");

            if (!implementation.Equals(CacheConstants.CacheImplementationRedis) && !implementation.Equals(CacheConstants.CacheImplementationMemcached))
                throw new Exception("Cache implementation specification is invalid");

            if (implementation.Equals(CacheConstants.CacheImplementationMemcached))
            {
                app.UseMemcached();
            }

            return app;
        }
    }
}