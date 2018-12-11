using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using HCore.Storage.Providers.Impl;
using HCore.Storage.Providers;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class StorageServiceCollectionExtensions
    {
        public static IServiceCollection AddStorage(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IStorageClientProvider, StorageClientProviderImpl>();

            return services;
        }
    }    
}
