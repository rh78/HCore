using HCore.Tenants;
using HCore.Tenants.Database.SqlServer;
using HCore.Tenants.Middleware;
using HCore.Tenants.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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

                    List<string> developerWildcardSubdomains = tenantDataProvider.DeveloperWildcardSubdomains;

                    developerWildcardSubdomains.Add("http://localhost");
                    developerWildcardSubdomains.Add("http://127.0.0.1:8080");
                    developerWildcardSubdomains.Add("https://localhost");
                    developerWildcardSubdomains.Add("https://127.0.0.1:8080");

                    List<Regex> developerWildcardSubdomainRegexes = developerWildcardSubdomains.Select(developerWildcardSubdomain =>
                        new Regex("^" + Regex.Escape(developerWildcardSubdomain).Replace("\\*", ".*") + "$")).ToList();

                    app.UseCors(builder =>
                    {
                        builder.AllowAnyHeader();
                        builder.AllowAnyMethod();
                        builder.AllowCredentials();

                        builder.SetIsOriginAllowed((origin) =>
                        {
                            return developerWildcardSubdomainRegexes.Any(developerWildcardSubdomainRegex =>
                                developerWildcardSubdomainRegex.IsMatch(origin));
                        });
                    });
                }
            }

            return app;
        }        
    }
}