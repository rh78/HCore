using HCore.Rest.Client;
using HCore.Rest.Client.Impl;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RestServiceCollectionExtensions
    {
        public static IServiceCollection AddRest(this IServiceCollection services)
        {
            services.AddScoped<IRestSharpClient, RestSharpClientImpl>();
            services.AddScoped<IRestSharpClientProvider, RestSharpClientProviderImpl>();

            return services;
        }
    }
}
