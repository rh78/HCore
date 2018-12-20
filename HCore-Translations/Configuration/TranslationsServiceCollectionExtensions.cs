using HCore.Translations.Providers;
using HCore.Translations.Providers.Impl;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TranslationsServiceCollectionExtensions
    {
        public static IServiceCollection AddCoreTranslations(this IServiceCollection services)
        {            
            services.AddSingleton<ITranslationsProvider, TranslationsProviderImpl>();

            services.AddSingleton<IStringLocalizerProvider, MessagesStringLocalizerProviderImpl>();

            return services;
        }
    }    
}
