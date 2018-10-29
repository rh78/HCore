using HCore.Tenants;

namespace Microsoft.AspNetCore.Http
{
    public static class HttpContextExtensions
    {
        public static ITenantInfo GetTenantInfo(this HttpContext context)
        {
            object tenantInfo = null;
            context.Items.TryGetValue(TenantsConstants.TenantInfoContextKey, out tenantInfo);

            return (ITenantInfo)tenantInfo;
        }
    }
}
