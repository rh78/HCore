using HCore.Tenants.Database.SqlServer;
using HCore.Tenants.Providers;
using HCore.Tenants.Providers.Impl;
using HCore.Tenants.Services;
using HCore.Tenants.Services.Impl;
using MessagePack.Formatters;
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

            services.AddSingleton<IMessagePackFormatter, TenantInfoMessagePackFormatterImpl>();
            services.AddSingleton<ITenantDataProvider, TenantDataProviderImpl>();
           
            services.TryAddScoped(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.GetTenantInfo());
            services.TryAddSingleton<ITenantInfoAccessor, TenantInfoAccessorImpl>();

            services.AddScoped<IUrlProvider, UrlProviderImpl>();
            var descriptor = new ServiceDescriptor(typeof(HCore.Web.Providers.IUrlProvider), typeof(UrlProviderImpl), ServiceLifetime.Scoped);

            services.Replace(descriptor);

            services.AddScoped<INonHttpContextUrlProvider, NonHttpContextUrlProviderImpl>();
            descriptor = new ServiceDescriptor(typeof(HCore.Web.Providers.INonHttpContextUrlProvider), typeof(NonHttpContextUrlProviderImpl), ServiceLifetime.Scoped);

            services.Replace(descriptor);

            services.AddScoped<IStorageClientProvider, StorageClientProviderImpl>();
            descriptor = new ServiceDescriptor(typeof(HCore.Storage.Providers.IStorageClientProvider), typeof(StorageClientProviderImpl), ServiceLifetime.Scoped);

            services.Replace(descriptor);

            bool useCors = configuration.GetValue<bool>("Identity:Tenants:UseCors");

            if (useCors)
            {
                services.AddCors();
            }

            services.AddScoped<ITenantServices, TenantServicesImpl>();

            return new TenantsBuilder(services);
        }
    }    
}
