using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HCore.Web.Middleware
{
    internal class CspHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CspHandlingMiddleware> _logger;

        public CspHandlingMiddleware(RequestDelegate next, ILogger<CspHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // see https://anthonychu.ca/post/aspnet-core-csp/

            context.Response.Headers["Content-Security-Policy"] = 
                "default-src 'self' 'unsafe-inline'; " +
                "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                "font-src 'self' https://fonts.gstatic.com; " +
                "script-src 'self' 'unsafe-eval' 'unsafe-inline'; " +
                "connect-src 'self';";

            await _next.Invoke(context).ConfigureAwait(false);
        }            
    }
}
