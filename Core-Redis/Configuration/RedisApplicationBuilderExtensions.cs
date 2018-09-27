using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class RedisApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseRedis(this IApplicationBuilder app)
        {
            IDistributedCache distributedCache = app.ApplicationServices.GetRequiredService<IDistributedCache>();

            // test the cache

            distributedCache.GetString("dummy:1");

            return app;
        }
    }
}