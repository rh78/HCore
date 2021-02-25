using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static HCore.Metadata.ILanguageMetadataProvider;

namespace HCore.Metadata.Impl
{
    internal class LanguageMetadataProviderImpl : ILanguageMetadataProvider
    {
        private readonly ILogger<LanguageMetadataProviderImpl> _logger;

        private readonly Dictionary<string, List<LanguageCodeNameMapping>> _localizedLanguageNameMappings = new Dictionary<string, List<LanguageCodeNameMapping>>()
        {
            {
                "en",
                new List<LanguageCodeNameMapping>()
            },
            {
                "de",
                new List<LanguageCodeNameMapping>()
            }
        };

        private readonly Dictionary<string, Dictionary<string, LanguageCodeNameMapping>> _lookupLocalizedLanguageNameMappings = new Dictionary<string, Dictionary<string, LanguageCodeNameMapping>>();

        public LanguageMetadataProviderImpl(ILogger<LanguageMetadataProviderImpl> logger)
        {
            _logger = logger;

            CultureInfo[] supportedCultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures)
                .Where(cultureInfo => !string.IsNullOrEmpty(cultureInfo.Name))
                .ToArray();

            _lookupLocalizedLanguageNameMappings.Add("en", new Dictionary<string, LanguageCodeNameMapping>());
            _lookupLocalizedLanguageNameMappings.Add("de", new Dictionary<string, LanguageCodeNameMapping>());

            var enCulture = CultureInfo.GetCultureInfo("en");
            var deCulture = CultureInfo.GetCultureInfo("de");

            foreach (var culture in supportedCultures)
            {
                if (culture.Parent != null &&
                    // iv is the invariant culture, the "root of all things"
                    !string.Equals(culture.Parent.TwoLetterISOLanguageName, "iv"))
                {
                    // there is sub languages (az-cyrl e.g.) which is a neutral culture but
                    // is not supported by us

                    continue;
                }

                var languageCode = culture.TwoLetterISOLanguageName;

                string languageNameEn;

                try
                {
                    languageNameEn = IsoNames.LanguageNames.GetName(enCulture, languageCode);
                }
                catch (KeyNotFoundException)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(languageNameEn))
                    throw new Exception($"Language name for {languageCode} is missing (EN)");

                var languageNameDe = IsoNames.LanguageNames.GetName(deCulture, languageCode);

                if (string.IsNullOrEmpty(languageNameDe))
                    throw new Exception($"Language name for {languageCode} is missing (DE)");

                _localizedLanguageNameMappings["en"].Add(new LanguageCodeNameMapping()
                {
                    LanguageCode = languageCode,
                    Name = languageNameEn
                });

                _localizedLanguageNameMappings["de"].Add(new LanguageCodeNameMapping()
                {
                    LanguageCode = languageCode,
                    Name = languageNameDe
                });

                _lookupLocalizedLanguageNameMappings["en"].Add(languageCode, new LanguageCodeNameMapping()
                {
                    LanguageCode = languageCode,
                    Name = languageNameEn
                });

                _lookupLocalizedLanguageNameMappings["de"].Add(languageCode, new LanguageCodeNameMapping()
                {
                    LanguageCode = languageCode,
                    Name = languageNameDe
                });
            }

            _localizedLanguageNameMappings["en"].Sort((language1, language2) =>
            {
                return string.Compare(language1.Name, language2.Name);
            });

            _localizedLanguageNameMappings["de"].Sort((language1, language2) =>
            {
                return string.Compare(language1.Name, language2.Name);
            });
        }

        public List<LanguageCodeNameMapping> GetLanguageList()
        {
            string currentCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            if (string.Equals(currentCulture, "de"))
                return _localizedLanguageNameMappings["de"];

            return _localizedLanguageNameMappings["en"];
        }

        public string GetLanguageName(string languageCode)
        {
            string currentCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            if (string.Equals(currentCulture, "de"))
            {
                if (_lookupLocalizedLanguageNameMappings["de"].ContainsKey(languageCode))
                    return _lookupLocalizedLanguageNameMappings["de"][languageCode].Name;
            }

            if (_lookupLocalizedLanguageNameMappings["en"].ContainsKey(languageCode))
                return _lookupLocalizedLanguageNameMappings["en"][languageCode].Name;

            return null;
        }
    }
}
