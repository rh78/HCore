using HCore.Tenants;
using HCore.Tenants.Models;

namespace Microsoft.AspNetCore.Http
{
    public static class HttpContextExtensions
    {
        public static ITenantInfo GetTenantInfo(this HttpContext context)
        {
            object tenantInfo = null;

            context.Items.TryGetValue(TenantConstants.TenantInfoContextKey, out tenantInfo);

            return (ITenantInfo)tenantInfo;
        }

        public static string GetMatchedSubDomain(this HttpContext context)
        {
            object matchedSubDomain = null;

            context.Items.TryGetValue(TenantConstants.MatchedSubDomainContextKey, out matchedSubDomain);

            return (string)matchedSubDomain;
        }
    }
}
