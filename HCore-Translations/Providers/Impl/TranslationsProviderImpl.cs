using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;

namespace HCore.Translations.Providers.Impl
{
    internal class TranslationsProviderImpl : ITranslationsProvider
    {
        private readonly IServiceProvider _serviceProvider;

        private List<IStringLocalizer> _stringLocalizers;

        private readonly ITranslationsProviderExtension _translationsProviderExtension;

        private Dictionary<string, string> _cachedJson = new Dictionary<string, string>();

        public TranslationsProviderImpl(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            var stringLocalizerProviderServices = _serviceProvider.GetServices<IStringLocalizerProvider>();

            _stringLocalizers = stringLocalizerProviderServices.Select(stringLocalizerProvider => stringLocalizerProvider.StringLocalizer).ToList();

            _translationsProviderExtension = _serviceProvider.GetService<ITranslationsProviderExtension>();
        }

        public string GetString(string key) {
            if (string.IsNullOrEmpty(key))
                return null;

            if (_translationsProviderExtension != null)
            {
                var providerExtensionText = _translationsProviderExtension.GetString(key);

                if (!string.IsNullOrEmpty(providerExtensionText))
                {
                    return providerExtensionText;
                }
            }

            foreach(var stringLocalizer in _stringLocalizers)
            {
                string text = stringLocalizer.GetString(key);
                if (text != null && !string.Equals(text, key))
                    return text;
            }

            return key;
        }   
        
        public string GetJson()
        {
            if (_translationsProviderExtension != null)
            {
                var providerExtensionJson = _translationsProviderExtension.GetJson();

                if (!string.IsNullOrEmpty(providerExtensionJson))
                {
                    return providerExtensionJson;
                }
            }

            string currentCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            if (_cachedJson.ContainsKey(currentCulture))
                return _cachedJson[currentCulture];

            StringBuilder jsonBuilder = new StringBuilder();

            jsonBuilder.Append("{");

            _stringLocalizers.ForEach(stringLocalizer =>
            {
                var translatedStrings = stringLocalizer.GetAllStrings(true);

                translatedStrings.ToList().ForEach(translatedString =>
                {
                    jsonBuilder
                        .Append("\"")
                        .Append(translatedString.Name)
                        .Append("\": \"")
                        .Append(HttpUtility.JavaScriptStringEncode(translatedString.Value))
                        .Append("\",\n");
                });
            });

            jsonBuilder.Append("}");

            string json = jsonBuilder.ToString();

            _cachedJson[currentCulture] = json;

            return json;
        }

        public string TranslateError(string errorCode, string errorMessage, string uuid, string name)
        {
            if (string.IsNullOrEmpty(errorCode))
                return errorMessage;

            string translatedErrorMessage = GetString(errorCode);
            if (string.IsNullOrEmpty(translatedErrorMessage) ||
                string.Equals(translatedErrorMessage, errorCode))
            {
                // not found

                return errorMessage;
            }

            if (!string.IsNullOrEmpty(uuid) && !string.IsNullOrEmpty(translatedErrorMessage))
                translatedErrorMessage = translatedErrorMessage.Replace("{uuid}", uuid);

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(translatedErrorMessage))
                translatedErrorMessage = translatedErrorMessage.Replace("{name}", name);

            return translatedErrorMessage;
        }
    }
}
