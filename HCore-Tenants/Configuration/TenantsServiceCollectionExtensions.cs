using HCore.Tenants;
using HCore.Tenants.Database.SqlServer;
using HCore.Tenants.Impl;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TenantsServiceCollectionExtensions
    {
        public static IServiceCollection AddTenants<TStartup>(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSqlServer<TStartup, SqlServerTenantDbContext>("Tenant", configuration);

            services.AddSingleton<ITenantDataProvider, TenantDataProviderImpl>();

            return services;
        }
    }    
}
