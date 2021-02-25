using Microsoft.Extensions.Configuration;
using System;
using MaxMind.GeoIP2;
using System.Reflection;
using HCore.Metadata;
using HCore.Metadata.Impl;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MetadataServiceCollectionExtensions
    {
        public static IServiceCollection AddMetadata(this IServiceCollection services, IConfiguration configuration)
        {
            Console.WriteLine("Initializing metadata...");

            services.AddSingleton<IGeoIP2DatabaseReader>((serviceProvider) =>
            {
                var currentAssembly = Assembly.GetExecutingAssembly();

                var resourceStream = currentAssembly.GetManifestResourceStream("HCore.Metadata.Resources.GeoLite2-Country.mmdb");

                if (resourceStream == null)
                    throw new Exception("GeoLite2 country database was not found");

                var databaseReader = new DatabaseReader(resourceStream);

                return databaseReader;
            });

            services.AddSingleton<ICountryMetadataProvider, CountryMetadataProviderImpl>();
            services.AddSingleton<ILanguageMetadataProvider, LanguageMetadataProviderImpl>();

            Console.WriteLine("Metadata initialized successfully");

            return services;
        }
    }
}
