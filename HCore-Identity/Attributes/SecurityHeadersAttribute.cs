using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace HCore.Identity.Attributes
{
    // see https://github.com/IdentityServer/IdentityServer4.Samples/blob/release/Quickstarts/Combined_AspNetIdentity_and_EntityFrameworkStorage/src/IdentityServerWithAspIdAndEF/Quickstart/SecurityHeadersAttribute.cs

    /// <summary>
    /// Adds special HTTP header to a response.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The HTTP headers to set are taken from the configuration with key
    /// <code>WebServer:http:headers:default:security</code> (see: <see cref="SecurityHeadersBaseConfigKey"/>).
    /// </para>
    /// <para>
    /// If a configuration key hold an empty value, then the HTTP header will not be sent to the client.
    /// </para>
    /// <para>
    /// Only missing headers are set to the response. If a header has been set to the response previously, then the
    /// configuration will be ignored for this response.
    /// </para>
    /// </remarks>
    public class SecurityHeadersAttribute : ActionFilterAttribute
    {
        public static string SecurityHeadersBaseConfigKey = "WebServer:http:headers:default:security";

        public static string CspHeaderBaseConfigKey = "WebServer:http:headers:default:security-csp";

        private readonly IConfiguration _configuration;

        public SecurityHeadersAttribute(IConfiguration configuration)
        {
            this._configuration = configuration;
        }

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            var result = context.Result;

            if (result is ViewResult || result is PageResult || result is LocalRedirectResult)
            {
                AddSecurityHeaders(context);

                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Content-Type-Options
                if (!context.HttpContext.Response.Headers.ContainsKey("X-Content-Type-Options"))
                {
                    context.HttpContext.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                }

                if (!context.HttpContext.Response.Headers.ContainsKey("P3P"))
                {
                    context.HttpContext.Response.Headers.Add("P3P", "CP=\"This is not a P3P policy!\"");
                }

                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Security-Policy
                var csp = GetConfiguredCspHeader() ?? "default-src 'self' https://*.smint.io https://*.smint.io; " +
                          "object-src 'none'; " +
                          "frame-ancestors 'self' https://*.smint.io:40443 https://*.smint.io https://*.sharepoint.com https://*.officeapps.live.com; " +
                          "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://*.smint.io https://code.jquery.com https://unpkg.com https://w.chatlio.com https://js.pusher.com https://cdn.segment.com https://www.google.com https://www.gstatic.com https://*.pusher.com https://appsforoffice.microsoft.com; " +
                          "connect-src 'self' *; " +
                          "style-src 'self' 'unsafe-inline' https://*.smint.io https://fonts.googleapis.com https://unpkg.com https://w.chatlio.com; " +
                          "font-src 'self' 'unsafe-inline' data: https://*.smint.io https://fonts.gstatic.com https://w.chatlio.com; " +
                          "frame-src 'self' https://*.smint.io:40443 https://*.smint.io https://www.google.com; " +
                          "img-src * data:; " +
                          "media-src *; " +
                          "sandbox allow-forms allow-same-origin allow-scripts allow-popups allow-popups-to-escape-sandbox; " +
                          "base-uri 'self'; " +
                          "upgrade-insecure-requests;";

                // also an example if you need client images to be displayed from twitter
                // csp += "img-src 'self' https://pbs.twimg.com;";

                // once for standards compliant browsers
                if (!context.HttpContext.Response.Headers.ContainsKey("Content-Security-Policy"))
                {
                    context.HttpContext.Response.Headers.Add("Content-Security-Policy", csp);
                }

                // IE just does trouble when opening PDFs and downloads, so we cannot use it right now

                // and once again for IE
                /* if (!context.HttpContext.Response.Headers.ContainsKey("X-Content-Security-Policy"))
                {
                    context.HttpContext.Response.Headers.Add("X-Content-Security-Policy", csp);
                } */

                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Referrer-Policy
                if (!context.HttpContext.Response.Headers.ContainsKey("Referrer-Policy"))
                {
                    context.HttpContext.Response.Headers.Add("Referrer-Policy", "no-referrer");
                }

                if (!context.HttpContext.Response.Headers.ContainsKey("Feature-Policy"))
                {
                    context.HttpContext.Response.Headers.Add("Feature-Policy", "autoplay: *; max-downscaling-image: *; unsized-media: *; animations: *; vertical-scroll: 'self';");
                }

                if (!context.HttpContext.Response.Headers.ContainsKey("X-XSS-Protection"))
                {
                    context.HttpContext.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
                }
            }
        }

        private string GetConfiguredCspHeader()
        {
            var configSection = _configuration?.GetSection(CspHeaderBaseConfigKey);
            var cspParts = configSection?.AsEnumerable();

            if (cspParts == null)
            {
                return null;
            }

            string cspHeader = null;

            foreach (var cspPart in cspParts)
            {
                var configName = cspPart.Key;
                var value = cspPart.Value;

                if (!configName.StartsWith(CspHeaderBaseConfigKey))
                {
                    continue;
                }

                var name = configName.Substring(CspHeaderBaseConfigKey.Length);
                if (name.StartsWith(":"))
                {
                    name = name.Substring(1);
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var printableValue = string.IsNullOrWhiteSpace(value) ? "" : " " + value;
                cspHeader = $"{cspHeader ?? ""}{name}{printableValue}; ";
            }

            return cspHeader;
        }

        private void AddSecurityHeaders(ResultExecutingContext context)
        {
            var configSection = _configuration?.GetSection(SecurityHeadersBaseConfigKey);
            var httpHeaders = configSection?.AsEnumerable();

            if (httpHeaders != null)
            {
                foreach (var header in httpHeaders)
                {
                    var configName = header.Key;
                    var value = header.Value;

                    var name = configName.Substring(SecurityHeadersBaseConfigKey.Length);
                    if (name.StartsWith(":"))
                    {
                        name = name.Substring(1);
                    }

                    if (!string.IsNullOrWhiteSpace(name) && !context.HttpContext.Response.Headers.ContainsKey(name))
                    {
                        context.HttpContext.Response.Headers.Add(name, value);
                    }
                }
            }
        }
    }
}