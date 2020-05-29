using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HCore.Identity.Attributes
{
    // see https://github.com/IdentityServer/IdentityServer4.Samples/blob/release/Quickstarts/Combined_AspNetIdentity_and_EntityFrameworkStorage/src/IdentityServerWithAspIdAndEF/Quickstart/SecurityHeadersAttribute.cs

    public class SecurityHeadersAttribute : ActionFilterAttribute
    {
        private bool _useSandbox;

        public SecurityHeadersAttribute(bool useSandbox = true)
        {
            _useSandbox = useSandbox;
        }

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            var result = context.Result;

            if (result is ViewResult || result is PageResult || result is LocalRedirectResult)
            {
                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Content-Type-Options
                if (!context.HttpContext.Response.Headers.ContainsKey("X-Content-Type-Options"))
                {
                    context.HttpContext.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                }

                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Security-Policy
                var csp = "default-src 'self' https://*.smint.io https://*.smint.io; " +
                          "object-src 'none'; " +
                          "frame-ancestors 'self' https://*.smint.io:40443 https://*.smint.io https://*.sharepoint.com https://*.officeapps.live.com; " +
                          "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://*.smint.io https://code.jquery.com https://unpkg.com https://w.chatlio.com https://js.pusher.com https://cdn.segment.com https://www.google.com https://www.gstatic.com https://*.pusher.com https://appsforoffice.microsoft.com; " +
                          "connect-src 'self' *; " +
                          "style-src 'self' 'unsafe-inline' https://*.smint.io https://fonts.googleapis.com https://unpkg.com https://w.chatlio.com; " +
                          "font-src 'self' 'unsafe-inline' data: https://*.smint.io https://fonts.gstatic.com https://w.chatlio.com; " +
                          "frame-src 'self' https://*.smint.io:40443 https://*.smint.io https://www.google.com; " +
                          "img-src * data:; " +
                          "media-src *; " +
                          // does have issues in Chrome version 83.0.4103.61 - just blocks downloads, disregarding the flags set
                          // we turn it off until more is known
                          // (_useSandbox ? "sandbox allow-forms allow-same-origin allow-scripts allow-popups allow-popups-to-escape-sandbox; " : "") +
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
    }
}