using MaxMind.GeoIP2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nager.Country;
using System;
using System.Collections.Generic;
using System.Globalization;
using static HCore.Metadata.ICountryMetadataProvider;

namespace HCore.Metadata.Impl
{
    internal class CountryMetadataProviderImpl : ICountryMetadataProvider
    {
        private readonly ILogger<CountryMetadataProviderImpl> _logger;

        private readonly ICountryProvider _countryProvider;
        private readonly IGeoIP2DatabaseReader _geoIP2DatabaseReader;

        private readonly Dictionary<string, List<CountryCodeNameMapping>> _localizedCountryNameMappings = new Dictionary<string, List<CountryCodeNameMapping>>()
        {
            {
                "en",
                new List<CountryCodeNameMapping>()
            },
            {
                "de",
                new List<CountryCodeNameMapping>()
            }
        };

        private readonly HashSet<string> _validCountryCodes = new HashSet<string>();

        public CountryMetadataProviderImpl(IServiceProvider serviceProvider, ILogger<CountryMetadataProviderImpl> logger)
        {
            _logger = logger;

            _countryProvider = new CountryProvider();

            var countries = _countryProvider.GetCountries();

            foreach (var country in countries)
            {
                var alpha2Code = country.Alpha2Code;

                var countryCode = Enum.GetName(typeof(Alpha2Code), alpha2Code).ToLower();

                var countryNameEn = _countryProvider.GetCountryTranslatedName(alpha2Code, LanguageCode.EN);

                if (string.IsNullOrEmpty(countryNameEn))
                    throw new Exception($"Country name for {alpha2Code} is missing (EN)");

                var countryNameDe = _countryProvider.GetCountryTranslatedName(alpha2Code, LanguageCode.DE);

                if (string.IsNullOrEmpty(countryNameDe))
                    throw new Exception($"Country name for {alpha2Code} is missing (DE)");

                _localizedCountryNameMappings["en"].Add(new CountryCodeNameMapping()
                {
                    CountryCode = countryCode,
                    Name = countryNameEn
                });

                _localizedCountryNameMappings["de"].Add(new CountryCodeNameMapping()
                {
                    CountryCode = countryCode,
                    Name = countryNameDe
                });

                _validCountryCodes.Add(countryCode);
            }

            _localizedCountryNameMappings["en"].Sort((country1, country2) =>
            {
                return string.Compare(country1.Name, country2.Name);
            });

            _localizedCountryNameMappings["de"].Sort((country1, country2) =>
            {
                return string.Compare(country1.Name, country2.Name);
            });

            _geoIP2DatabaseReader = serviceProvider.GetRequiredService<IGeoIP2DatabaseReader>();
        }

        public List<CountryCodeNameMapping> GetCountryList()
        {
            string currentCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            if (string.Equals(currentCulture, "de"))
                return _localizedCountryNameMappings["de"];

            return _localizedCountryNameMappings["en"];
        }

        public string GetValidatedCountryCode(string countryCode)
        {
            if (_validCountryCodes.Contains(countryCode))
                return countryCode;

            return null;
        }

        public string GetDefaultCultureForCountry(string countryCode)
        {
            if (string.Equals(countryCode, "de") ||
                string.Equals(countryCode, "at") ||
                string.Equals(countryCode, "ch"))
            {
                return "de";
            }

            return "en";
        }

        public string GetDefaultCurrencyForCountry(string countryCode)
        {
            var countryInfo = _countryProvider.GetCountry(countryCode);

            if (countryInfo == null)
                return "usd";

            var currencies = countryInfo.Currencies;

            if (currencies == null || currencies.Length == 0)
                return "usd";

            var defaultCurrency = currencies[0];

            if (string.Equals(defaultCurrency, "EUR"))
                return "eur";

            return "usd";
        }

        public string GetCountryCodeForIpAddress(string ipAddress)
        {
            try
            {
                var location = _geoIP2DatabaseReader.Country(ipAddress);

                return location.Country.IsoCode.ToLower();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool IsEuropeanUnionCountry(string countryCode)
        {
            return string.Equals(countryCode, "be") ||
                    string.Equals(countryCode, "bg") ||
                    string.Equals(countryCode, "cz") ||
                    string.Equals(countryCode, "dk") ||
                    string.Equals(countryCode, "de") ||
                    string.Equals(countryCode, "ee") ||
                    string.Equals(countryCode, "ie") ||
                    string.Equals(countryCode, "el") ||
                    string.Equals(countryCode, "es") ||
                    string.Equals(countryCode, "fr") ||
                    string.Equals(countryCode, "hr") ||
                    string.Equals(countryCode, "it") ||
                    string.Equals(countryCode, "cy") ||
                    string.Equals(countryCode, "lv") ||
                    string.Equals(countryCode, "lt") ||
                    string.Equals(countryCode, "lu") ||
                    string.Equals(countryCode, "hu") ||
                    string.Equals(countryCode, "mt") ||
                    string.Equals(countryCode, "nl") ||
                    string.Equals(countryCode, "pl") ||
                    string.Equals(countryCode, "pt") ||
                    string.Equals(countryCode, "ro") ||
                    string.Equals(countryCode, "si") ||
                    string.Equals(countryCode, "sk") ||
                    string.Equals(countryCode, "fi") ||
                    string.Equals(countryCode, "se"); // TODO (?)
        }
    }
}
