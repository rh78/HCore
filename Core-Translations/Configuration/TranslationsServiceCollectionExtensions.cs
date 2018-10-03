using ReinhardHolzner.Core.Translations;
using ReinhardHolzner.Core.Translations.Impl;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TranslationsServiceCollectionExtensions
    {
        public static IServiceCollection AddCoreTranslations(this IServiceCollection services)
        {            
            services.AddSingleton<ITranslationsProvider, TranslationsProviderImpl>();

            services.AddSingleton<IStringLocalizerProvider, ErrorCodesStringLocalizerProviderImpl>();

            return services;
        }
    }    
}
