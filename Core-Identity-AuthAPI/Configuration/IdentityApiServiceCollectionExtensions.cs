using ReinhardHolzner.Core.Identity.AuthAPI.Controllers.API.Impl;
using ReinhardHolzner.Core.Identity.AuthAPI.Generated.Controllers;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IdentityApiServiceCollectionExtensions
    {
        public static IServiceCollection AddIdentityApi(this IServiceCollection services)
        {
            services.AddScoped<ISecureApiController, SecureApiImpl>();

            return services;
        }        
    }
}
