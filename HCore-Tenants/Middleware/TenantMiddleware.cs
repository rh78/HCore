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

        private readonly List<string> _devAdminSsoReplacementWhitelistIpAddresses = new List<string>();

        private readonly ILogger<TenantsMiddleware> _logger;

        public TenantsMiddleware(RequestDelegate next, ITenantDataProvider tenantDataProvider, ILogger<TenantsMiddleware> logger, IConfiguration configuration)
        {
            _next = next;

            _tenantDataProvider = tenantDataProvider;

            // GetValue not working with lists, see:
            // https://stackoverflow.com/questions/47832661/configuration-getvalue-list-returns-null
            // https://github.com/aspnet/Configuration/issues/451

            var devAdminSsoReplacementWhitelistIpAddresses = new List<string>();

            configuration.GetSection("Identity:Tenants:DevAdminSsoReplacementWhitelistIpAddresses")?.Bind(devAdminSsoReplacementWhitelistIpAddresses);

            devAdminSsoReplacementWhitelistIpAddresses.ForEach((devAdminSsoReplacementWhitelistIpAddress) =>
            {
                if (!string.IsNullOrEmpty(devAdminSsoReplacementWhitelistIpAddress))
                {
                    _devAdminSsoReplacementWhitelistIpAddresses.Add(devAdminSsoReplacementWhitelistIpAddress);
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
                    
                    (matchedSubDomain, tenantInfo) = await _tenantDataProvider.GetTenantByHostAsync(host).ConfigureAwait(false);

                    if (tenantInfo != null && tenantInfo.RequiresDevAdminSsoReplacement)
                    {
                        if (_devAdminSsoReplacementWhitelistIpAddresses != null &&
                            _devAdminSsoReplacementWhitelistIpAddresses.Count > 0)
                        {
                            var ipAddress = context.Connection?.RemoteIpAddress?.ToString();

                            if (_devAdminSsoReplacementWhitelistIpAddresses.Contains(ipAddress))
                            {
                                tenantInfo = tenantInfo.Clone();

                                if (!string.IsNullOrEmpty(tenantInfo.DevAdminSsoReplacementSamlPeerEntityId))
                                    tenantInfo.SamlPeerEntityId = tenantInfo.DevAdminSsoReplacementSamlPeerEntityId;

                                if (!string.IsNullOrEmpty(tenantInfo.DevAdminSsoReplacementSamlPeerIdpMetadataLocation))
                                {
                                    tenantInfo.SamlPeerIdpMetadataLocation = tenantInfo.DevAdminSsoReplacementSamlPeerIdpMetadataLocation;
                                    tenantInfo.SamlPeerIdpMetadata = null;
                                }

                                tenantInfo.AdditionalCacheKey = "devAdminSsoReplacement";
                            }
                        }
                    }
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
                            (matchedSubDomain, tenantInfo) = await _tenantDataProvider.GetTenantByHostAsync(healthCheckTenantHost).ConfigureAwait(false);
                        }
                    }

                    if (tenantInfo == null)
                    {
                        _logger.LogInformation($"No tenant found for host {hostString}");

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
