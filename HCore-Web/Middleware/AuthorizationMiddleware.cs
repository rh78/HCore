using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HCore.Web.Middleware
{
    internal class AuthorizationMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly AuthorizationMiddlewareOptions _options;

        private readonly IAuthorizationService _authorizationService;        

        private readonly ILogger<AuthorizationMiddleware> _logger;

        public AuthorizationMiddleware(
            RequestDelegate next,
            IAuthorizationService authorizationService,
            ILogger<AuthorizationMiddleware> logger,
            IOptions<AuthorizationMiddlewareOptions> optionsAccessor)
            : this(next, authorizationService, logger, optionsAccessor.Value)
        { }

        public AuthorizationMiddleware(
            RequestDelegate next,
            IAuthorizationService authorizationService,
            ILogger<AuthorizationMiddleware> logger,
            AuthorizationMiddlewareOptions options)
        { 
            _next = next;

            _authorizationService = authorizationService;

            _options = options ?? new AuthorizationMiddlewareOptions();

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Invoke(HttpContext context)
        {
            var httpMethod = context.Request.Method;
            var path = context.Request.Path.Value;

            // If the RoutePrefix is requested (with or without trailing slash), redirect to index URL

            if (httpMethod == "GET" && path.StartsWith(_options.RoutePrefix))
            {                
                var authorizationResult = await _authorizationService.AuthorizeAsync(context.User, _options.PolicyName).ConfigureAwait(false);

                if (!authorizationResult.Succeeded)
                {
                    var authenticationScheme = _options.AuthenticationScheme;

                    if (!string.IsNullOrEmpty(authenticationScheme))
                    {
                        await context.ChallengeAsync(authenticationScheme).ConfigureAwait(false);
                    }
                    else
                    {
                        await context.ChallengeAsync().ConfigureAwait(false);
                    }

                    return;
                }
            }

            await _next.Invoke(context).ConfigureAwait(false);
        }            
    }
}
