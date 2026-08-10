using HCore.Pusher.Messenger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class PusherApplicationBuilderExtensions
    {
        public static IApplicationBuilder UsePusher(this IApplicationBuilder app, IConfiguration configuration)
        {
            var usePusher = configuration.GetValue<bool?>("Pusher:UsePusher") ?? false;

            if (usePusher)
            {
                app.ApplicationServices.GetRequiredService<IPusherMessenger>();
            }

            return app;
        }
    }
}