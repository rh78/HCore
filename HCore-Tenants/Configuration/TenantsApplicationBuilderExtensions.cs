using HCore.Tenants;
using HCore.Tenants.Database.SqlServer;
using HCore.Tenants.Middleware;
using HCore.Tenants.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Builder
{
    public static class TenantsApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseTenants(this IApplicationBuilder app, bool migrate = true)
        {
            app.UseSqlDatabase<SqlServerTenantDbContext>(migrate);

            var tenantDataProvider = app.ApplicationServices.GetRequiredService<ITenantDataProvider>();

            app.UseMiddleware<TenantsMiddleware>();

            var scopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();

            using (var scope = scopeFactory.CreateScope())
            {
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                bool useCors = configuration.GetValue<bool>("Identity:Tenants:UseCors");

                if (useCors)
                {
                    HashSet<string> originUrls = new HashSet<string>();

                    tenantDataProvider.Tenants.ForEach(tenant =>
                    {
                        string url = tenant.WebUrl;
                        if (url.EndsWith("/"))
                            url = url.Substring(0, url.Length - 1);

                        originUrls.Add(url);
                    });

                    originUrls.Add("http://localhost");
                    originUrls.Add("https://localhost");

                    app.UseCors(builder =>
                    {
                        builder.AllowAnyHeader();
                        builder.AllowAnyMethod();
                        builder.AllowCredentials();

                        builder.WithOrigins(originUrls.ToArray());
                    });
                }
            }

            return app;
        }        
    }
}