using HCore.Templating.Renderer;
using HCore.Templating.Renderer.Impl;

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
