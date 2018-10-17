using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HCore.Translations.Impl
{
    internal class TranslationsProviderImpl : ITranslationsProvider
    {
        private readonly IServiceProvider _serviceProvider;

        private List<IStringLocalizer> _stringLocalizers;

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
    }
}
