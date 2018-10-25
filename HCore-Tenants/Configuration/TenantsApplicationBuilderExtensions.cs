using HCore.Tenants;
using HCore.Tenants.Database.SqlServer;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class TenantsApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseTenants(this IApplicationBuilder app)
        {
            app.UseSqlServer<SqlServerTenantDbContext>();

            app.ApplicationServices.GetRequiredService<ITenantDataProvider>();

            return app;
        }        
    }
}