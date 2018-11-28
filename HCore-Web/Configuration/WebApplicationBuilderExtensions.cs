using System;
using HCore.Web.Middleware;

namespace Microsoft.AspNetCore.Builder
{
    public static class WebApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseAuthorizationMiddleware(this IApplicationBuilder app, Action<AuthorizationMiddlewareOptions> setupAction = null)
        {
            if (setupAction == null)
            {
                // Don't pass options so it can be configured/injected via DI container instead

                app.UseMiddleware<AuthorizationMiddleware>();
            }
            else
            {
                // Configure an options instance here and pass directly to the middleware

                var options = new AuthorizationMiddlewareOptions();
                setupAction.Invoke(options);

                app.UseMiddleware<AuthorizationMiddleware>(options);
            }

            return app;
        }        
    }
}