using Microsoft.Extensions.DependencyInjection;
using HCore.Translations.Providers;
using System;

namespace Microsoft.AspNetCore.Builder
{
    public static class TranslationsApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseCoreTranslations(this IApplicationBuilder app)
        {
            var translationsProvider = app.ApplicationServices.GetRequiredService<ITranslationsProvider>();

            string translation = translationsProvider.GetString("access_token_expired");

            if (string.IsNullOrEmpty(translation) || string.Equals(translation, "access_token_expired"))
                throw new Exception("Translation can not be read");            

            return app;
        }        
    }
}