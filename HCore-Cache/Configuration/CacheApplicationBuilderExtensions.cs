using HCore.Cache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

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

            ICache cache = app.ApplicationServices.GetRequiredService<ICache>();

            // test the cache

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            cache.StoreAsync("dummy:1", "value", TimeSpan.FromHours(5)).Wait();
            var task = cache.GetAsync<string>("dummy:1");

            task.Wait();

            string value = task.Result;
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

            if (!string.Equals(value, "value"))
                throw new Exception("Cache does not work");

            return app;
        }
    }
}