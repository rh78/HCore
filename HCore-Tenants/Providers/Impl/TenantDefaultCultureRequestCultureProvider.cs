using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using System.Threading.Tasks;

namespace HCore.Tenants.Providers.Impl
{
    internal class TenantDefaultCultureRequestCultureProvider : RequestCultureProvider
    {
        public override Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext context)
        {
            var tenantInfo = context.GetTenantInfo();

            if (tenantInfo != null)
            {
                // handle request culture

                if (!string.IsNullOrEmpty(tenantInfo.DefaultCulture))
                {
                    // if the ASP.NET Core request cookie is not present
                    // and the "culture" parameter in the query is not present

                    // set the thread's culture to the tenant default culture

                    return Task.FromResult(new ProviderCultureResult(tenantInfo.DefaultCulture));
                }
            }

            return NullProviderCultureResult;
        }
    }
}
