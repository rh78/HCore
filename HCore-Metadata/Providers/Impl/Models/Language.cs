using System.Collections.Generic;

namespace HCore.Metadata.Providers.Impl.Models
{
    internal class Language
    {
        public string LanguageCode { get; private set; }

        private Dictionary<string, string> _translations;

        public Language(string languageCode, string englishTranslation, string germanTranslation)
            : this(languageCode, englishTranslation, germanTranslation, englishTranslation, englishTranslation, englishTranslation)
        {
        }

        public Language(string languageCode, string englishTranslation, string germanTranslation, string spanishTranslation, string portugueseTranslation, string italianTranslation)
        {
            LanguageCode = languageCode;

            _translations = new Dictionary<string, string>();

            _translations["en"] = englishTranslation;
            _translations["de"] = germanTranslation;
            _translations["es"] = spanishTranslation;
            _translations["pt"] = portugueseTranslation;
            _translations["it"] = italianTranslation;
        }

        public string GetTranslation(string twoLetterIsoLanguageName)
        {
            if (_translations.TryGetValue(twoLetterIsoLanguageName, out var translation))
            {
                return translation;
            }

            return _translations["en"];
        }
    }
}
