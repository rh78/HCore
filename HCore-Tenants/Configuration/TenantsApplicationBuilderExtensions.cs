using HCore.Tenants;
using HCore.Tenants.Database.SqlServer;
using HCore.Tenants.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class TenantsApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseTenants(this IApplicationBuilder app)
        {
            app.UseSqlServer<SqlServerTenantDbContext>();

            app.ApplicationServices.GetRequiredService<ITenantDataProvider>();

            app.UseMiddleware<TenantsMiddleware>();

            return app;
        }        
    }
}