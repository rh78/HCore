using HCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class MetadataApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseMetadata(this IApplicationBuilder app)
        {
            // check if instantiation works

            app.ApplicationServices.GetRequiredService<ICountryMetadataProvider>();

            return app;
        }
    }
}