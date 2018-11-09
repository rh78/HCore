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

        private Dictionary<string, string> _cachedJson = new Dictionary<string, string>();

        public TranslationsProviderImpl(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            var stringLocalizerProviderServices = _serviceProvider.GetServices<IStringLocalizerProvider>();

            _stringLocalizers = stringLocalizerProviderServices.Select(stringLocalizerProvider => stringLocalizerProvider.StringLocalizer).ToList();            
        }

        public string GetString(string key) {
            if (string.IsNullOrEmpty(key))
                return null;

            foreach(var stringLocalizer in _stringLocalizers)
            {
                string text = stringLocalizer.GetString(key);
                if (!string.IsNullOrEmpty(text))
                    return text;
            }

            return key;
        }   
        
        public string GetJson()
        {
            string currentCulture = CultureInfo.CurrentCulture.ToString();

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
    }
}
