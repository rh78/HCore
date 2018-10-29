using HCore.Web.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HCore.Tenants.Middleware
{
    internal class TenantsMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly ITenantDataProvider _tenantDataProvider;

        private readonly ILogger<TenantsMiddleware> _logger;

        public TenantsMiddleware(RequestDelegate next, ITenantDataProvider tenantDataProvider, ILogger<TenantsMiddleware> logger)
        {
            _next = next;

            _tenantDataProvider = tenantDataProvider;

            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.Items.ContainsKey(TenantsConstants.TenantInfoContextKey))
            {
                ITenantInfo tenantInfo = null;

                var hostString = context.Request.Host;

                string host = null;

                if (hostString.HasValue)
                {
                    host = hostString.Host;
                    
                    tenantInfo = _tenantDataProvider.LookupTenant(host);
                }

                if (tenantInfo == null)
                {
                    // we could not find any tenant

                    _logger.LogError($"No tenant found for host {hostString}");

                    var apiException = new NotFoundApiException(NotFoundApiException.TenantNotFound, $"The tenant for host {host} was not found");

                    await apiException.WriteResponseAsync(context).ConfigureAwait(false);

                    return;
                }

                context.Items.Add(TenantsConstants.TenantInfoContextKey, tenantInfo);                
            }

            await _next.Invoke(context).ConfigureAwait(false);
        }
    }
}
