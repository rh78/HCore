using HCore.Tenants.Database.SqlServer;
using HCore.Tenants.Providers;
using HCore.Tenants.Providers.Impl;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TenantsServiceCollectionExtensions
    {
        public static TenantsBuilder AddTenants<TStartup>(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSqlDatabase<TStartup, SqlServerTenantDbContext>("Tenant", configuration);

            services.AddSingleton<ITenantDataProvider, TenantDataProviderImpl>();
           
            services.TryAddScoped(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.GetTenantInfo());
            services.TryAddSingleton<ITenantInfoAccessor, TenantInfoAccessorImpl>();

            services.AddScoped<HCore.Tenants.Providers.IUrlProvider, UrlProviderImpl>();
            var descriptor = new ServiceDescriptor(typeof(HCore.Web.Providers.IUrlProvider), typeof(UrlProviderImpl), ServiceLifetime.Scoped);

            services.Replace(descriptor);

            bool useCors = configuration.GetValue<bool>("Identity:Tenants:UseCors");

            if (useCors)
            {
                services.AddCors();
            }

            return new TenantsBuilder(services);
        }
    }    
}
