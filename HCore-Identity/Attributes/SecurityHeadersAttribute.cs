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

                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Security-Policy
                var csp = GetConfiguredCspHeader();

                // once for standards compliant browsers
                if (
                    !string.IsNullOrWhiteSpace(csp)
                    && !context.HttpContext.Response.Headers.ContainsKey("Content-Security-Policy")
                )
                {
                    context.HttpContext.Response.Headers.Add("Content-Security-Policy".ToLower(), csp);
                }

                // IE just does trouble when opening PDFs and downloads, so we cannot use it right now

                // and once again for IE
                /* if (
                    !string.IsNullOrWhiteSpace(csp) 
                    && !context.HttpContext.Response.Headers.ContainsKey("X-Content-Security-Policy")
                )
                {
                    context.HttpContext.Response.Headers.Add("X-Content-Security-Policy", csp);
                } */
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

                var name = configName.Substring(CspHeaderBaseConfigKey.Length).ToLower();
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

                    var name = configName.Substring(SecurityHeadersBaseConfigKey.Length).ToLower();
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