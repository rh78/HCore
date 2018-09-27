using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ReinhardHolzner.Core.Web.Middleware
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

        public async Task Invoke(HttpContext context)
        {
            // see https://anthonychu.ca/post/aspnet-core-csp/

            context.Response.Headers.Add("Content-Security-Policy",
                   "default-src 'self' 'unsafe-inline' " +
                   "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com " +
                   "font-src 'self' https://fonts.gstatic.com");

            await _next.Invoke(context).ConfigureAwait(false);
        }            
    }
}
