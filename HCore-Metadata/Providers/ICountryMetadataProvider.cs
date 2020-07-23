using System.Collections.Generic;

namespace HCore.Metadata
{
    public interface ICountryMetadataProvider
    {
        public class CountryCodeNameMapping
        {
            public string CountryCode { get; internal set; }
            public string Name { get; internal set; }
        }

        public List<CountryCodeNameMapping> GetCountryList();

        string GetValidatedCountryCode(string countryCode);

        string GetDefaultCultureForCountry(string countryCode);
        string GetDefaultCurrencyForCountry(string countryCode);

        string GetCountryCodeForIpAddress(string ipAddress);

        bool IsEuropeanUnionCountry(string countryCode);
    }
}
