using HCore.Rest;
using HCore.Rest.Impl;

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
