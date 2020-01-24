using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HCore.Identity.Attributes
{
    // see https://github.com/IdentityServer/IdentityServer4.Samples/blob/release/Quickstarts/Combined_AspNetIdentity_and_EntityFrameworkStorage/src/IdentityServerWithAspIdAndEF/Quickstart/SecurityHeadersAttribute.cs

    public class SecurityHeadersAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            var result = context.Result;
            if (result is ViewResult || result is PageResult)
            {
                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Content-Type-Options
                if (!context.HttpContext.Response.Headers.ContainsKey("X-Content-Type-Options"))
                {
                    context.HttpContext.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                }

                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Frame-Options
                if (!context.HttpContext.Response.Headers.ContainsKey("X-Frame-Options"))
                {
                    context.HttpContext.Response.Headers.Add("X-Frame-Options", "sameorigin");
                }

                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Security-Policy
                var csp = "default-src 'self' https://*.smint.io https://*.smint.io; " +
                          "object-src 'none'; " +
                          "frame-ancestors 'none'; " +
                          "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://*.smint.io https://code.jquery.com https://unpkg.com https://w.chatlio.com https://js.pusher.com https://cdn.segment.com https://www.google.com https://www.gstatic.com; " +
                          "connect-src 'self' https://*.smint.io https://*.smint.io:40444 https://*.smint.io:41444 https://api.chatlio.com https://api-cdn.chatlio.com wss://ws.pusherapp.com https://api.segment.com https://api.segment.io; " +
                          "style-src 'self' 'unsafe-inline' https://*.smint.io https://fonts.googleapis.com https://unpkg.com https://w.chatlio.com; " +
                          "font-src 'self' 'unsafe-inline' data: https://*.smint.io https://fonts.gstatic.com https://w.chatlio.com; " +
                          "frame-src 'self' https://*.smint.io https://www.google.com; " +
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
    }
}