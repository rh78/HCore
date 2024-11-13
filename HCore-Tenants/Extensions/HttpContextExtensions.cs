using System;
using System.Net;
using HCore.Tenants;
using HCore.Tenants.Models;

namespace Microsoft.AspNetCore.Http
{
    public static class HttpContextExtensions
    {
        public static ITenantInfo GetTenantInfo(this HttpContext context)
        {
            object tenantInfo = null;

            context.Items.TryGetValue(TenantConstants.TenantInfoContextKey, out tenantInfo);

            return (ITenantInfo)tenantInfo;
        }

        public static string GetMatchedSubDomain(this HttpContext context)
        {
            object matchedSubDomain = null;

            context.Items.TryGetValue(TenantConstants.MatchedSubDomainContextKey, out matchedSubDomain);

            return (string)matchedSubDomain;
        }

        public static string GetIpAddress(this HttpContext context)
        {
            if (context == null)
            {
                return null;
            }

            var request = context.Request;

            if (request != null && request.Headers != null)
            {
                if (request.Headers.TryGetValue("HC-Connecting-IP", out var hcoreConnectingIp) &&
                    !string.IsNullOrEmpty(hcoreConnectingIp) &&
                    IPAddress.TryParse(hcoreConnectingIp, out var hcoreIpParsed) &&
                    hcoreIpParsed != null)
                {
                    return hcoreIpParsed.ToString();
                }

                if (request.Headers.TryGetValue("CF-Connecting-IP", out var cloudflareConnectingIp) &&
                    !string.IsNullOrEmpty(cloudflareConnectingIp) &&
                    IPAddress.TryParse(cloudflareConnectingIp, out var cloudflareIpParsed) &&
                    cloudflareIpParsed != null)
                {
                    return cloudflareIpParsed.ToString();
                }
            }

            var connection = context.Connection;

            if (connection != null)
            {
                try
                {
                    var ipAddress = connection.RemoteIpAddress?.ToString();

                    return ipAddress;
                }
                catch (Exception)
                {
                    // ignore
                }
            }

            return null;
        }
    }
}
