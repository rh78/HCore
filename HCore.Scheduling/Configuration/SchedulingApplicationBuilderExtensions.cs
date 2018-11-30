using HCore.Scheduling.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class SchedulingApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseScheduling(this IApplicationBuilder app)
        {
            app.ApplicationServices.GetRequiredService<ISchedulingProvider>();

            return app;
        }
    }
}