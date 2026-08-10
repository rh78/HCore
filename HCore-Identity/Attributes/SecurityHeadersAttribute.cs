using System;
using System.Configuration;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace HCore.Identity.Attributes
{
    // see https://github.com/IdentityServer/IdentityServer4.Samples/blob/release/Quickstarts/Combined_AspNetIdentity_and_EntityFrameworkStorage/src/IdentityServerWithAspIdAndEF/Quickstart/SecurityHeadersAttribute.cs

    public class SecurityHeadersAttribute : ActionFilterAttribute
    {
        private string _defaultSrcPolicy;
        private string _frameAncestorsPolicy;
        private string _scriptSrcPolicy;
        private string _nonceScriptSrcPolicy;
        private string _styleSrcPolicy;
        private string _fontSrcPolicy;
        private string _connectSrcPolicy;
        private string _frameSrcPolicy;
        private string _imgSrcPolicy;
        private string _mediaSrcPolicy;
        private string _reportUri;

        public SecurityHeadersAttribute(IConfiguration configuration)
        {
            _defaultSrcPolicy = GetConfiguration(configuration, "WebServer:Csp:DefaultSrc");
            _frameAncestorsPolicy = GetConfiguration(configuration, "WebServer:Csp:FrameAncestors");
            _scriptSrcPolicy = GetConfiguration(configuration, "WebServer:Csp:ScriptSrc");
            _nonceScriptSrcPolicy = GetConfiguration(configuration, "WebServer:Csp:NonceScriptSrc");
            _styleSrcPolicy = GetConfiguration(configuration, "WebServer:Csp:StyleSrc");
            _fontSrcPolicy = GetConfiguration(configuration, "WebServer:Csp:FontSrc");
            _connectSrcPolicy = GetConfiguration(configuration, "WebServer:Csp:ConnectSrc");
            _frameSrcPolicy = GetConfiguration(configuration, "WebServer:Csp:FrameSrc");
            _imgSrcPolicy = GetConfiguration(configuration, "WebServer:Csp:ImgSrc");
            _mediaSrcPolicy = GetConfiguration(configuration, "WebServer:Csp:MediaSrc");
            _reportUri = configuration["WebServer:Csp:ReportUri"];
        }

        private string GetConfiguration(IConfiguration configuration, string key)
        {
            var section = configuration.GetSection(key);

            if (section == null)
            {
                return "";
            }

            var values = section.Get<string[]>();

            values = values?
                .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct()
                .ToArray();

            if (values == null || !values.Any())
            {
                return "";
            }

            return string.Join(" ", values);
        }

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            var result = context.Result;

            if (result is ViewResult || result is PageResult || result is LocalRedirectResult)
            {
                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Content-Type-Options
                if (!context.HttpContext.Response.Headers.ContainsKey("X-Content-Type-Options"))
                {
                    context.HttpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
                }

                if (!context.HttpContext.Response.Headers.ContainsKey("P3P"))
                {
                    context.HttpContext.Response.Headers["P3P"] = "CP=\"This is not a P3P policy!\"";
                }

                var allowIFrameUrl = "";

                if (context.HttpContext.Items.ContainsKey(IdentityCoreConstants.AllowIFrameUrlContextKey))
                {
                    var allowIFrameUrlInner = (string)context.HttpContext.Items[IdentityCoreConstants.AllowIFrameUrlContextKey];

                    if (!string.IsNullOrEmpty(allowIFrameUrlInner))
                    {
                        allowIFrameUrl = $" {allowIFrameUrlInner}";
                    }
                }

                var scriptNonce = context.HttpContext.GetScriptNonce();

                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Security-Policy

                // also an example if you need client images to be displayed from twitter
                // csp += "img-src 'self' https://pbs.twimg.com;";

                // once for standards compliant browsers

                if (!context.HttpContext.Response.Headers.ContainsKey("Content-Security-Policy"))
                {
                    var csp = $"default-src 'self' {_defaultSrcPolicy}; " +
                            "object-src 'none'; " +
                            $"frame-ancestors 'self' {_frameAncestorsPolicy} {allowIFrameUrl}; " +
                            $"script-src 'self' {_scriptSrcPolicy};" +
                            $"connect-src 'self' {_connectSrcPolicy}; " +
                            $"style-src 'self' {_styleSrcPolicy}; " +
                            $"font-src 'self' {_fontSrcPolicy}; " +
                            $"frame-src 'self' {_frameSrcPolicy}; " +
                            $"img-src {_imgSrcPolicy}; " +
                            $"media-src {_mediaSrcPolicy}; " +
                            // does have issues in Chrome version 83.0.4103.61 - just blocks downloads, disregarding the flags set
                            // we turn it off until more is known
                            // (_useSandbox ? "sandbox allow-forms allow-same-origin allow-scripts allow-popups allow-popups-to-escape-sandbox; " : "") +
                            "base-uri 'self'; " +
                            "upgrade-insecure-requests;";

                    context.HttpContext.Response.Headers["Content-Security-Policy"] = csp;
                }

                if (!context.HttpContext.Response.Headers.ContainsKey("Content-Security-Policy-Report-Only"))
                {
                    var cspReportOnly = $"default-src 'self' {_defaultSrcPolicy}; " +
                            "object-src 'none'; " +
                            $"frame-ancestors 'self' {_frameAncestorsPolicy} {allowIFrameUrl}; " +
                            $"script-src 'self' 'nonce-{scriptNonce}' 'strict-dynamic' {_nonceScriptSrcPolicy};" +
                            $"connect-src 'self' {_connectSrcPolicy}; " +
                            $"style-src 'self' {_styleSrcPolicy}; " +
                            $"font-src 'self' {_fontSrcPolicy}; " +
                            $"frame-src 'self' {_frameSrcPolicy}; " +
                            $"img-src {_imgSrcPolicy}; " +
                            $"media-src {_mediaSrcPolicy}; " +
                            // does have issues in Chrome version 83.0.4103.61 - just blocks downloads, disregarding the flags set
                            // we turn it off until more is known
                            // (_useSandbox ? "sandbox allow-forms allow-same-origin allow-scripts allow-popups allow-popups-to-escape-sandbox; " : "") +
                            "base-uri 'none'; " +
                            "upgrade-insecure-requests; " +
                            $"report-uri {_reportUri};";

                    context.HttpContext.Response.Headers["Content-Security-Policy-Report-Only"] = cspReportOnly;
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
                    context.HttpContext.Response.Headers["Referrer-Policy"] = "no-referrer";
                }

                if (!context.HttpContext.Response.Headers.ContainsKey("Permissions-Policy"))
                {
                    context.HttpContext.Response.Headers["Permissions-Policy"] = "geolocation=(),camera=(),microphone=(),clipboard-read=(self),clipboard-write=(self),vertical-scroll=(self),fullscreen=*,autoplay=*";
                }
            }
        }
    }
}