using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using HCore.Redis;

namespace Microsoft.AspNetCore.Builder
{
    public static class RedisApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseRedis(this IApplicationBuilder app)
        {
            IRedisCache redisCache = app.ApplicationServices.GetRequiredService<IRedisCache>();

            // test the cache

            redisCache.GetAsync<object>("dummy:1");

            return app;
        }
    }
}