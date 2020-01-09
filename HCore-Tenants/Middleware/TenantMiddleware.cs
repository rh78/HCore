using HCore.Tenants.Models;
using HCore.Tenants.Providers;
using HCore.Web.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace HCore.Tenants.Middleware
{
    internal class TenantsMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly ITenantDataProvider _tenantDataProvider;

        private string _tenantSelectorFallbackUrl;

        private readonly ILogger<TenantsMiddleware> _logger;

        public TenantsMiddleware(RequestDelegate next, ITenantDataProvider tenantDataProvider, ILogger<TenantsMiddleware> logger, IConfiguration configuration)
        {
            _next = next;

            _tenantDataProvider = tenantDataProvider;

            _tenantSelectorFallbackUrl = configuration["Identity:Tenants:TenantSelectorFallbackUrl"];

            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.Items.ContainsKey(TenantConstants.TenantInfoContextKey))
            {
                ITenantInfo tenantInfo = null;

                var hostString = context.Request.Host;

                string host = null;

                if (hostString.HasValue)
                {
                    host = hostString.Host;
                    
                    tenantInfo = _tenantDataProvider.LookupTenantByHost(host);
                }

                if (tenantInfo == null)
                {
                    // we could not find any tenant

                    // check if we have a health check running here

                    var healthCheckPort = _tenantDataProvider.HealthCheckPort;

                    if (healthCheckPort != null &&
                        context.Request.Host.Port == healthCheckPort)
                    {
                        var healthCheckTenantHost = _tenantDataProvider.HealthCheckTenantHost;

                        if (!string.IsNullOrEmpty(healthCheckTenantHost))
                        {
                            tenantInfo = _tenantDataProvider.LookupTenantByHost(healthCheckTenantHost);
                        }
                    }

                    if (tenantInfo == null)
                    {
                        _logger.LogError($"No tenant found for host {hostString}");

                        throw new NotFoundApiException(NotFoundApiException.TenantNotFound, $"The tenant for host {host} was not found", host);
                    }
                } 
                else if (tenantInfo.TenantUuid == 0)
                {
                    if (string.IsNullOrEmpty(_tenantSelectorFallbackUrl))
                        throw new NotFoundApiException(NotFoundApiException.TenantNotFound, $"The tenant for host {host} was not found", host);

                    // only allow the tenant selector fallback URL on the tenant selector tenant

                    var path = context.Request.GetEncodedUrl();

                    if (string.IsNullOrEmpty(path) ||
                        (!path.EndsWith(".css") && !string.Equals(path, _tenantSelectorFallbackUrl)))
                    {
                        throw new NotFoundApiException(NotFoundApiException.TenantNotFound, $"The tenant for host {host} was not found", host);
                    }
                }

                context.Items.Add(TenantConstants.TenantInfoContextKey, tenantInfo);
            }

            await _next.Invoke(context).ConfigureAwait(false);
        }
    }
}
