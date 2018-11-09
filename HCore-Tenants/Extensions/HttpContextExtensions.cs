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
    }
}
