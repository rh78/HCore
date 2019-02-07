using HCore.Segment.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class SegmentApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseSegment(this IApplicationBuilder app)
        {
            app.ApplicationServices.GetRequiredService<ISegmentProvider>();

            return app;
        }
    }
}