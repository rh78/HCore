using HCore.Templating.Generic;
using HCore.Templating.Generic.Impl;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TemplatingServiceCollectionExtensions
    {
        public static IServiceCollection AddTemplating(this IServiceCollection services)
        {
            services.AddScoped<ITemplateRenderer, TemplateRendererImpl>();

            return services;
        }
    }
}
