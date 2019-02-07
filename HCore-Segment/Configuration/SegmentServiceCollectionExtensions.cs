using HCore.Segment.Providers;
using HCore.Segment.Providers.Impl;
using Microsoft.Extensions.Configuration;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SegmentServiceCollectionExtensions
    {
        public static IServiceCollection AddSegment(this IServiceCollection services, IConfiguration configuration)
        {
            Console.WriteLine("Initializing Segment.io...");

            string apiKey = configuration["Segment:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                throw new Exception("Segment API key is empty");

            Segment.Analytics.Initialize(apiKey);

            services.AddSingleton<ISegmentProvider, SegmentProviderImpl>();

            Console.WriteLine("Segment.io initialized successfully");

            return services;
        }        
    }
}
