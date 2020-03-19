using System.Collections.Generic;
using System.Text.RegularExpressions;
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

        private readonly string _tenantSelectorFallbackUrl;

        private readonly List<Regex> _tenantSelectorWhitelistUrls = new List<Regex>();

        private readonly ILogger<TenantsMiddleware> _logger;

        public TenantsMiddleware(RequestDelegate next, ITenantDataProvider tenantDataProvider, ILogger<TenantsMiddleware> logger, IConfiguration configuration)
        {
            _next = next;

            _tenantDataProvider = tenantDataProvider;

            _tenantSelectorFallbackUrl = configuration["Identity:Tenants:TenantSelectorFallbackUrl"];

            // GetValue not working with lists, see:
            // https://stackoverflow.com/questions/47832661/configuration-getvalue-list-returns-null
            // https://github.com/aspnet/Configuration/issues/451

            var whitelistUrls = new List<string>();

            configuration.GetSection("Identity:Tenants:TenantSelectorWhitelistUrls")?.Bind(whitelistUrls);

            whitelistUrls.ForEach((whitelistedUrl) =>
            {
                if (!string.IsNullOrEmpty(whitelistedUrl))
                {
                    _tenantSelectorWhitelistUrls.Add(new Regex(whitelistedUrl, RegexOptions.Compiled));
                }
            });

            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Items.ContainsKey(TenantConstants.TenantInfoContextKey))
            {
                string matchedSubDomain = null;
                ITenantInfo tenantInfo = null;

                var hostString = context.Request.Host;

                string host = null;

                if (hostString.HasValue)
                {
                    host = hostString.Host;
                    
                    (matchedSubDomain, tenantInfo) = _tenantDataProvider.LookupTenantByHost(host);
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
                            (matchedSubDomain, tenantInfo) = _tenantDataProvider.LookupTenantByHost(healthCheckTenantHost);
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

                    var url = context.Request.GetEncodedUrl();

                    var isWhiteListed = false;

                    if (!string.IsNullOrEmpty(url))
                    {
                        isWhiteListed = url.EndsWith(".css");

                        _tenantSelectorWhitelistUrls.ForEach((whitelistedRegEx) =>
                        {
                            isWhiteListed |= whitelistedRegEx.IsMatch(url);
                        });
                    }

                    if (string.IsNullOrEmpty(url) ||
                        (!isWhiteListed && !string.Equals(url, _tenantSelectorFallbackUrl)))
                    {
                        throw new NotFoundApiException(NotFoundApiException.TenantNotFound, $"The tenant for host {host} was not found", host);
                    }
                }

                context.Items.Add(TenantConstants.TenantInfoContextKey, tenantInfo);
                context.Items.Add(TenantConstants.MatchedSubDomainContextKey, matchedSubDomain);
            }

            await _next.Invoke(context).ConfigureAwait(false);
        }
    }
}
