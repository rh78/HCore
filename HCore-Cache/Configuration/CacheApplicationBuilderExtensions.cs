using System;
using HCore.Cache;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Builder
{
    public static class CacheApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseCache(this IApplicationBuilder app, IConfiguration configuration)
        {
            string implementation = configuration[$"Cache:Implementation"];

            if (string.IsNullOrEmpty(implementation))
                throw new Exception("Cache implementation specification is empty");

            if (!implementation.Equals(CacheConstants.CacheImplementationRedis))
                throw new Exception("Cache implementation specification is invalid");

            return app;
        }
    }
}