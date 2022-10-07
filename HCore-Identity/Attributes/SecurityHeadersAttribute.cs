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

                if (!context.HttpContext.Response.Headers.ContainsKey("P3P"))
                {
                    context.HttpContext.Response.Headers.Add("P3P", "CP=\"This is not a P3P policy!\"");
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
                
                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Security-Policy
                var csp = "default-src 'self' https://*.smint.io:40443 https://*.smint.io https://smintiocdn.azureedge.net https://cdn.smint.io; " +
                          "object-src 'none'; " +
                          $"frame-ancestors 'self' https://*.smint.io:40443 https://*.smint.io https://*.sharepoint.com https://*.officeapps.live.com https://*.veevavault.com{allowIFrameUrl}; " +
                          "script-src 'self' 'unsafe-inline' 'unsafe-eval' blob: http://127.0.0.1:8000 https://development-host.smint.io:8443 https://*.smint.io:40443 https://*.smint.io https://smintiocdn.azureedge.net https://cdn.smint.io https://code.jquery.com https://unpkg.com https://w.chatlio.com https://js.pusher.com https://cdn.segment.com https://www.google.com https://www.gstatic.com https://*.pusher.com https://appsforoffice.microsoft.com https://snap.licdn.com https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://npmcdn.com https://maps.googleapis.com;" +
                          "connect-src 'self' *; " +
                          "style-src 'self' 'unsafe-inline' https://*.smint.io:40443 https://*.smint.io https://*.portalsapib.smint.io:43444 https://*.portalsapib.smint.io https://*.portalsapife.smint.io:43444 https://*.portalsapife.smint.io https://smintiocdn.azureedge.net https://cdn.smint.io https://fonts.googleapis.com https://unpkg.com https://w.chatlio.com https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
                          "font-src 'self' 'unsafe-inline' data: https://*.smint.io:40443 https://*.smint.io https://smintiodevcachecdn.azureedge.net https://staticcdn.smint.io https://smintiocdn.azureedge.net https://cdn.smint.io https://fonts.gstatic.com https://w.chatlio.com https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
                          "frame-src data: 'self' *; " +
                          "img-src * data:; " +
                          "media-src blob: *; " +
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