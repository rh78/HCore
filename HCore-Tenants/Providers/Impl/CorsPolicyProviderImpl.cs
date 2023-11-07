using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace HCore.Tenants.Providers.Impl
{
    internal class CorsPolicyProviderImpl : ICorsPolicyProvider
    {
        private static readonly CorsPolicy DefaultPolicy = new CorsPolicyBuilder()
            .AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed((origin) =>
            {
                // For Portals we need this open

                return true;
            })
            .AllowCredentials()
            .Build();

        public Task<CorsPolicy> GetPolicyAsync(HttpContext context, string policyName)
        {
            return Task.FromResult(DefaultPolicy);
        }
    }
}
