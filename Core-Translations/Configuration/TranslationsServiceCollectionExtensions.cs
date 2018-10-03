using ReinhardHolzner.Core.Translations;
using ReinhardHolzner.Core.Translations.Impl;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TranslationsServiceCollectionExtensions
    {
        public static IServiceCollection AddCoreTranslations(this IServiceCollection services)
        {            
            services.AddScoped<ITranslationsProvider, TranslationsProviderImpl>();

            services.AddScoped<IStringLocalizerProvider, ErrorCodesStringLocalizerProviderImpl>();

            return services;
        }
    }    
}
