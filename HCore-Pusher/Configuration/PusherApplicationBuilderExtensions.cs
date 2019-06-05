using HCore.Pusher.Messenger;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class PusherApplicationBuilderExtensions
    {
        public static IApplicationBuilder UsePusher(this IApplicationBuilder app)
        {
            app.ApplicationServices.GetRequiredService<IPusherMessenger>();

            return app;
        }
    }
}