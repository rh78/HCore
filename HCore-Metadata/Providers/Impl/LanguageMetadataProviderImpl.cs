using HCore.Metadata.Providers.Impl.Models;
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

        private static List<Language> Languages = new List<Language>()
        {
            new Language("aa", "Afar", "Danakil"),
            new Language("ab", "Abkhazian", "Abchasisch"),
            new Language("ae", "Avestan", "Avestisch"),
            new Language("af", "Afrikaans", "Afrikaans"),
            new Language("ak", "Akan", "Akan"),
            new Language("am", "Amharic", "Amharisch"),
            new Language("an", "Aragonese", "Aragonesisch"),
            new Language("ar", "Arabic", "Arabisch"),
            new Language("as", "Assamese", "Assamesisch"),
            new Language("av", "Avaric", "Awarisch"),
            new Language("ay", "Aymara", "Aymará"),
            new Language("az", "Azerbaijani", "Aserbeidschanisch"),
            new Language("ba", "Bashkir", "Baschkirisch"),
            new Language("be", "Belarusian", "Weißrussisch"),
            new Language("bg", "Bulgarian", "Bulgarisch"),
            new Language("bh", "Bihari", "Bihari"),
            new Language("bi", "Bislama", "Beach-la-mar"),
            new Language("bm", "Bambara", "Bambara"),
            new Language("bn", "Bengali", "Bengali"),
            new Language("bo", "Tibetan", "Tibetisch"),
            new Language("br", "Breton", "Bretonisch"),
            new Language("bs", "Bosnian", "Bosnisch"),
            new Language("ca", "Catalan", "Katalanisch"),
            new Language("ce", "Chechen", "Tschetschenisch"),
            new Language("ch", "Chamorro", "Chamorro"),
            new Language("co", "Corsican", "Korsisch"),
            new Language("cr", "Cree", "Cree"),
            new Language("cs", "Czech", "Tschechisch"),
            new Language("cv", "Chuvash", "Tschuwaschisch"),
            new Language("cy", "Welsh", "Kymrisch"),
            new Language("da", "Danish", "Dänisch"),
            new Language("de", "German", "Deutsch", "Alemana", "Alemão", "Tedesco"),
            new Language("dv", "Maldivian", "Maledivisch"),
            new Language("dz", "Dzongkha", "Dzongkha"),
            new Language("ee", "Ewe", "Ewe"),
            new Language("el", "Greek", "Griechisch"),
            new Language("en", "English", "Englisch", "Inglés", "Inglês", "Inglese"),
            new Language("eo", "Esperanto", "Esperanto"),
            new Language("es", "Spanish", "Spanisch", "Español", "Espanhol", "Spagnolo"),
            new Language("et", "Estonian", "Estnisch"),
            new Language("eu", "Basque", "Baskisch"),
            new Language("fa", "Persian", "Persisch"),
            new Language("ff", "Fulah", "Ful"),
            new Language("fi", "Finnish", "Finnisch"),
            new Language("fj", "Fijian", "Fidschi"),
            new Language("fo", "Faroese", "Färöisch"),
            new Language("fr", "French", "Französisch"),
            new Language("fy", "Frisian", "Friesisch"),
            new Language("ga", "Irish", "Irisch"),
            new Language("gl", "Galician", "Galizisch"),
            new Language("gn", "Guarani", "Guarani"),
            new Language("gu", "Gujarati", "Gujarati"),
            new Language("gv", "Manx", "Manx"),
            new Language("ha", "Hausa", "Haussa"),
            new Language("he", "Hebrew", "Hebräisch"),
            new Language("hi", "Hindi", "Hindi"),
            new Language("ho", "Hiri Motu", "Hiri-Motu"),
            new Language("hr", "Croatian", "Kroatisch"),
            new Language("ht", "Haitian", "Haitisch"),
            new Language("hu", "Hungarian", "Ungarisch"),
            new Language("hy", "Armenian", "Armenisch"),
            new Language("hz", "Herero", "Herero"),
            new Language("id", "Indonesian", "Indonesisch"),
            new Language("ig", "Igbo", "Ibo"),
            new Language("ii", "Nuosu", "Lalo"),
            new Language("ik", "Inupiaq", "Inupik"),
            new Language("io", "Ido", "Ido"),
            new Language("is", "Icelandic", "Isländisch"),
            new Language("it", "Italian", "Italienisch", "Italiano", "Italiano", "Italiano"),
            new Language("iu", "Inuktitut", "Inuktitut"),
            new Language("ja", "Japanese", "Japanisch"),
            new Language("jv", "Javanese", "Javanisch"),
            new Language("ka", "Georgian", "Georgisch"),
            new Language("kg", "Kongo", "Kongo"),
            new Language("ki", "Kikuyu", "Kikuyu"),
            new Language("kj", "Kwanyama", "Kwanyama"),
            new Language("kk", "Kazakh", "Kasachisch"),
            new Language("kl", "Greenlandic", "Grönländisch"),
            new Language("km", "Khmer", "Kambodschanisch"),
            new Language("kn", "Kannada", "Kannada"),
            new Language("ko", "Korean", "Koreanisch"),
            new Language("kr", "Kanuri", "Kanuri"),
            new Language("ks", "Kashmiri", "Kaschmiri"),
            new Language("ku", "Kurdish", "Kurdisch"),
            new Language("kv", "Komi", "Korni"),
            new Language("kw", "Cornish", "Kornisch"),
            new Language("ky", "Kirghiz", "Kirgisisch"),
            new Language("lb", "Luxembourgish", "Luxemburgisch"),
            new Language("lg", "Ganda", "Ganda"),
            new Language("li", "Limburgan", "Limburgisch"),
            new Language("ln", "Lingala", "Lingala"),
            new Language("lo", "Lao", "Laotisch"),
            new Language("lt", "Lithuanian", "Litauisch"),
            new Language("lu", "Luba-Katanga", "Luba-Katanga"),
            new Language("lv", "Latvian", "Lettisch"),
            new Language("mg", "Malagasy", "Malagassi"),
            new Language("mh", "Marshallese", "Marschallesisch"),
            new Language("mi", "Maori", "Maori"),
            new Language("mk", "Macedonian", "Makedonisch"),
            new Language("ml", "Malayalam", "Malayalam"),
            new Language("mn", "Mongolian", "Mongolisch"),
            new Language("mr", "Marathi", "Marathi"),
            new Language("ms", "Malay", "Malaiisch"),
            new Language("mt", "Maltese", "Maltesisch"),
            new Language("my", "Burmese", "Birmanisch"),
            new Language("na", "Nauru", "Nauruanisch"),
            new Language("nb", "Bokmål", "Bokmål"),
            new Language("nd", "Ndebele", "Ndebele"),
            new Language("ne", "Nepali", "Nepali"),
            new Language("ng", "Ndonga", "Ndonga"),
            new Language("nl", "Dutch", "Niederländisch"),
            new Language("no", "Norwegian", "Norwegisch"),
            new Language("nr", "Ndebele", "Ndebele"),
            new Language("nv", "Navajo", "Navajo"),
            new Language("ny", "Nyanja", "Nyanja"),
            new Language("oj", "Ojibwa", "Ojibwa"),
            new Language("om", "Oromo", "Galla"),
            new Language("or", "Oriya", "Oriya"),
            new Language("os", "Ossetian", "Ossetisch"),
            new Language("pa", "Panjabi", "Pandschabi"),
            new Language("pi", "Pali", "Pali"),
            new Language("pl", "Polish", "Polnisch"),
            new Language("ps", "Pushto", "Paschtu"),
            new Language("pt", "Portuguese", "Portugiesisch", "Portugués", "Português", "Portoghese"),
            new Language("qu", "Quechua", "Quechua"),
            new Language("rm", "Romansh", "Rätoromanisch"),
            new Language("rn", "Rundi", "Rundi"),
            new Language("ro", "Romanian", "Rumänisch"),
            new Language("ru", "Russian", "Russisch"),
            new Language("rw", "Kinyarwanda", "Rwanda"),
            new Language("sa", "Sanskrit", "Sanskrit"),
            new Language("sc", "Sardinian", "Sardisch"),
            new Language("sd", "Sindhi", "Sindhi"),
            new Language("se", "Northern Sami", "Nordsaamisch"),
            new Language("sg", "Sango", "Sango"),
            new Language("si", "Sinhala", "Singhalesisch"),
            new Language("sk", "Slovak", "Slowakisch"),
            new Language("sl", "Slovenian", "Slowenisch"),
            new Language("sm", "Samoan", "Samoanisch"),
            new Language("sn", "Shona", "Schona"),
            new Language("so", "Somali", "Somali"),
            new Language("sq", "Albanian", "Albanisch"),
            new Language("sr", "Serbian", "Serbisch"),
            new Language("ss", "Swati", "Swasi"),
            new Language("st", "Southern Sotho", "Süd-Sotho"),
            new Language("su", "Sundanese", "Sundanesisch"),
            new Language("sv", "Swedish", "Schwedisch"),
            new Language("sw", "Swahili", "Swahili"),
            new Language("ta", "Tamil", "Tamil"),
            new Language("te", "Telugu", "Telugu"),
            new Language("tg", "Tajik", "Tadschikisch"),
            new Language("th", "Thai", "Thailändisch"),
            new Language("ti", "Tigrinya", "Tigrinja"),
            new Language("tk", "Turkmen", "Turkmenisch"),
            new Language("tl", "Tagalog", "Tagalog"),
            new Language("tn", "Tswana", "Tswana"),
            new Language("to", "Tonga", "Tongaisch"),
            new Language("tr", "Turkish", "Türkisch"),
            new Language("ts", "Tsonga", "Tsonga"),
            new Language("tt", "Tatar", "Tatarisch"),
            new Language("ty", "Tahitian", "Tahitisch"),
            new Language("tw", "Twi", "Twi"),
            new Language("ug", "Uighur", "Uigurisch"),
            new Language("uk", "Ukrainian", "Ukrainisch"),
            new Language("ur", "Urdu", "Urdu"),
            new Language("uz", "Uzbek", "Usbekisch"),
            new Language("ve", "Venda", "Venda"),
            new Language("vi", "Vietnamese", "Vietnamesisch"),
            new Language("vo", "Volapük", "Volapük"),
            new Language("wa", "Walloon", "Wallonisch"),
            new Language("wo", "Wolof", "Wolof"),
            new Language("xh", "Xhosa", "Xhosa"),
            new Language("yi", "Yiddish", "Jiddisch"),
            new Language("yo", "Yoruba", "Yoruba"),
            new Language("za", "Zhuang", "Zhuang"),
            new Language("zh", "Chinese", "Chinesisch"),
            new Language("zu", "Zulu", "Zulu")
        };

        private static Dictionary<string, Language> LanguageDictionary = Languages.ToDictionary(language => language.LanguageCode, language => language);

        private readonly Dictionary<string, List<LanguageCodeNameMapping>> _localizedLanguageNameMappings = new Dictionary<string, List<LanguageCodeNameMapping>>()
        {
            {
                "en",
                new List<LanguageCodeNameMapping>()
            },
            {
                "de",
                new List<LanguageCodeNameMapping>()
            },
            {
                "es",
                new List<LanguageCodeNameMapping>()
            },
            {
                "pt",
                new List<LanguageCodeNameMapping>()
            },
            {
                "it",
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
            _lookupLocalizedLanguageNameMappings.Add("es", new Dictionary<string, LanguageCodeNameMapping>());
            _lookupLocalizedLanguageNameMappings.Add("pt", new Dictionary<string, LanguageCodeNameMapping>());
            _lookupLocalizedLanguageNameMappings.Add("it", new Dictionary<string, LanguageCodeNameMapping>());

            var enCulture = CultureInfo.GetCultureInfo("en");
            var deCulture = CultureInfo.GetCultureInfo("de");
            var esCulture = CultureInfo.GetCultureInfo("es");
            var ptCulture = CultureInfo.GetCultureInfo("pt");
            var itCulture = CultureInfo.GetCultureInfo("it");

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

                if (!LanguageDictionary.TryGetValue(languageCode, out var language))
                {
                    continue;
                }

                var languageNameEn = language.GetTranslation("en");

                if (string.IsNullOrEmpty(languageNameEn))
                    throw new Exception($"Language name for {languageCode} is missing (EN)");

                var languageNameDe = language.GetTranslation("de");

                if (string.IsNullOrEmpty(languageNameDe))
                    throw new Exception($"Language name for {languageCode} is missing (DE)");

                var languageNameEs = language.GetTranslation("es");

                if (string.IsNullOrEmpty(languageNameEs))
                    throw new Exception($"Language name for {languageCode} is missing (ES)");

                var languageNamePt = language.GetTranslation("pt");

                if (string.IsNullOrEmpty(languageNamePt))
                    throw new Exception($"Language name for {languageCode} is missing (PT)");

                var languageNameIt = language.GetTranslation("it");

                if (string.IsNullOrEmpty(languageNameIt))
                    throw new Exception($"Language name for {languageCode} is missing (IT)");

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

                _localizedLanguageNameMappings["es"].Add(new LanguageCodeNameMapping()
                {
                    LanguageCode = languageCode,
                    Name = languageNameEs
                });

                _localizedLanguageNameMappings["pt"].Add(new LanguageCodeNameMapping()
                {
                    LanguageCode = languageCode,
                    Name = languageNamePt
                });

                _localizedLanguageNameMappings["it"].Add(new LanguageCodeNameMapping()
                {
                    LanguageCode = languageCode,
                    Name = languageNameIt
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

                _lookupLocalizedLanguageNameMappings["es"].Add(languageCode, new LanguageCodeNameMapping()
                {
                    LanguageCode = languageCode,
                    Name = languageNameEs
                });

                _lookupLocalizedLanguageNameMappings["pt"].Add(languageCode, new LanguageCodeNameMapping()
                {
                    LanguageCode = languageCode,
                    Name = languageNamePt
                });

                _lookupLocalizedLanguageNameMappings["it"].Add(languageCode, new LanguageCodeNameMapping()
                {
                    LanguageCode = languageCode,
                    Name = languageNameIt
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

            _localizedLanguageNameMappings["es"].Sort((language1, language2) =>
            {
                return string.Compare(language1.Name, language2.Name);
            });

            _localizedLanguageNameMappings["pt"].Sort((language1, language2) =>
            {
                return string.Compare(language1.Name, language2.Name);
            });

            _localizedLanguageNameMappings["it"].Sort((language1, language2) =>
            {
                return string.Compare(language1.Name, language2.Name);
            });
        }

        public List<LanguageCodeNameMapping> GetLanguageList()
        {
            string currentCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            if (string.Equals(currentCulture, "de"))
                return _localizedLanguageNameMappings["de"];

            if (string.Equals(currentCulture, "es"))
                return _localizedLanguageNameMappings["es"];

            if (string.Equals(currentCulture, "pt"))
                return _localizedLanguageNameMappings["pt"];

            if (string.Equals(currentCulture, "it"))
                return _localizedLanguageNameMappings["it"];

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

            if (string.Equals(currentCulture, "es"))
            {
                if (_lookupLocalizedLanguageNameMappings["es"].ContainsKey(languageCode))
                    return _lookupLocalizedLanguageNameMappings["es"][languageCode].Name;
            }

            if (string.Equals(currentCulture, "pt"))
            {
                if (_lookupLocalizedLanguageNameMappings["pt"].ContainsKey(languageCode))
                    return _lookupLocalizedLanguageNameMappings["pt"][languageCode].Name;
            }

            if (string.Equals(currentCulture, "it"))
            {
                if (_lookupLocalizedLanguageNameMappings["it"].ContainsKey(languageCode))
                    return _lookupLocalizedLanguageNameMappings["it"][languageCode].Name;
            }

            if (_lookupLocalizedLanguageNameMappings["en"].ContainsKey(languageCode))
                return _lookupLocalizedLanguageNameMappings["en"][languageCode].Name;

            return null;
        }
    }
}
