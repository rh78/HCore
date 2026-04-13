using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using HCore.Identity.Database.SqlServer.Models.Impl;
using HCore.Identity.Extensions;
using HCore.Web.Attributes;
using IdentityModel;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace HCore.Identity.Controllers
{
    [ApiController]
    public partial class AuthorizationController : Controller
    {
        private readonly IOpenIddictApplicationManager _openIddictApplicationManager;

        private readonly UserManager<UserModel> _userManager;
        private readonly IUserClaimsPrincipalFactory<UserModel> _principalFactory;
        private readonly SignInManager<UserModel> _signInManager;

        private ILogger<AuthorizationController> _logger;

        public AuthorizationController(IOpenIddictApplicationManager applicationManager, UserManager<UserModel> userManager, IUserClaimsPrincipalFactory<UserModel> principalFactory, SignInManager<UserModel> signInManager, ILogger<AuthorizationController> logger)
        {
            _openIddictApplicationManager = applicationManager;

            _userManager = userManager;
            _principalFactory = principalFactory;
            _signInManager = signInManager;

            _logger = logger;
        }

        [HttpGet]
        [Route("/connect/authorize")]
        [HttpPost("/connect/authorize")]
        [Produces("application/json")]
        [ValidateModelState]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> AuthorizeAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var openIddictRequest = HttpContext.GetOpenIddictServerRequest();

            if (openIddictRequest == null)
            {
                throw new InvalidOperationException("Request could not be read");
            }

            var openIddictApplication = await _openIddictApplicationManager.FindByClientIdAsync(openIddictRequest.ClientId).ConfigureAwait(false);

            if (openIddictApplication == null)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidClient,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The client is invalid"
                    }));
            }

            var result = await HttpContext.AuthenticateAsync();

            if (!IsAuthenticated(result, openIddictRequest))
            {
                var parameters = ParseOAuthParameters(HttpContext, new List<string> { Parameters.Prompt });

                var redirectUrl = BuildRedirectUrl(HttpContext.Request, parameters);

                return LocalRedirect(redirectUrl);
            }

            // Retrieve the profile of the logged in user.

            var user = await _userManager.GetUserAsync(result.Principal);

            if (user == null)
            {
                throw new InvalidOperationException("The user details could not be retrieved");
            }

            var identity = new ClaimsIdentity(
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: Claims.Name,
                roleType: Claims.Role
            );

            identity.SetClaim(Claims.Subject, user.Id);

            identity.SetScopes(openIddictRequest.GetScopes());

            identity.SetDestinations(GetDestinations);

            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        [HttpGet]
        [Route("/connect/token")]
        [HttpPost("/connect/token")]
        [Produces("application/json")]
        [ValidateModelState]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> TokenAsync()
        {
            var openIddictRequest = HttpContext.GetOpenIddictServerRequest();

            if (openIddictRequest == null)
            {
                throw new InvalidOperationException("Request could not be read");
            }

            if (!openIddictRequest.IsAuthorizationCodeGrantType() &&
                !openIddictRequest.IsClientCredentialsGrantType() &&
                !openIddictRequest.IsRefreshTokenGrantType())
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.UnsupportedGrantType,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The grant type is not supported"
                    }));
            }

            var openIddictApplication = await _openIddictApplicationManager.FindByClientIdAsync(openIddictRequest.ClientId).ConfigureAwait(false);

            if (openIddictApplication == null)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidClient,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The client is invalid"
                    }));
            }

            ClaimsIdentity identity = new ClaimsIdentity(
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: Claims.Name,
                roleType: Claims.Role
            );

            IActionResult actionResult;

            if (openIddictRequest.IsAuthorizationCodeGrantType() || openIddictRequest.IsRefreshTokenGrantType())
            {
                actionResult = await HandleAuthorizationCodeGrantTypeAsync(openIddictRequest, identity).ConfigureAwait(false);
            }
            else if (openIddictRequest.IsClientCredentialsGrantType())
            {
                actionResult = await HandleClientCredentialsGrantTypeAsync(openIddictApplication, identity).ConfigureAwait(false);
            }
            else
            {
                // should never happen

                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.UnsupportedGrantType,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The grant type is not supported"
                    }));
            }

            if (actionResult != null)
            {
                return actionResult;
            }

            // finalize identity

            identity.SetClaim("idp", "local");

            var openIddictApplicationSettings = await _openIddictApplicationManager.GetSettingsAsync(openIddictApplication).ConfigureAwait(false);
            var clientClaims = openIddictApplicationSettings?.GetClaimsSettings()?.ClientClaims;

            if (clientClaims != null && clientClaims.Any())
            {
                foreach (var clientClaimKeyValuePair in clientClaims)
                {
                    identity.SetClaim(clientClaimKeyValuePair.Key, clientClaimKeyValuePair.Value);
                }
            }

            identity.SetClaim(JwtClaimTypes.JwtId, CryptoRandom.CreateUniqueId(16));

            identity.SetScopes(openIddictRequest.GetScopes());

            identity.SetDestinations(GetDestinations);

            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        private async Task<IActionResult> HandleAuthorizationCodeGrantTypeAsync(OpenIddictRequest openIddictRequest, ClaimsIdentity identity)
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            if (!IsAuthenticated(authenticateResult, openIddictRequest))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.AccessDenied,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Access denied"
                    }));
            }

            var claimsPrincipal = authenticateResult.Principal;

            var subject = GetSubjectId(claimsPrincipal);

            if (string.IsNullOrEmpty(subject))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.AccessDenied,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Access denied"
                    }));
            }

            var userModel = await GetUserAsync(subject).ConfigureAwait(false);

            if (userModel == null)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.AccessDenied,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Access denied"
                    }));
            }

            // set claim

            identity.SetClaim(Claims.Subject, userModel.Id);

            // add claims from identity principal

            var identityPricipal = await _principalFactory.CreateAsync(userModel).ConfigureAwait(false);

            var developerAdminClaimValues = identityPricipal.GetClaims(IdentityCoreConstants.DeveloperAdminClaim);

            if (developerAdminClaimValues != null && developerAdminClaimValues.Any())
            {
                identity.SetClaims(IdentityCoreConstants.DeveloperAdminClaim, developerAdminClaimValues.ToImmutableArray());
            }

            var apiDocsClaimValues = identityPricipal.GetClaims("api_docs");

            if (apiDocsClaimValues != null && apiDocsClaimValues.Any())
            {
                identity.SetClaims("api_docs", apiDocsClaimValues.ToImmutableArray());
            }

            var isAdcuClaimValues = identityPricipal.GetClaims("is_adcu");

            if (isAdcuClaimValues != null && isAdcuClaimValues.Any())
            {
                identity.SetClaims("is_adcu", apiDocsClaimValues.ToImmutableArray());
            }

            return null;
        }

        private async Task<IActionResult> HandleClientCredentialsGrantTypeAsync(object openIddictApplication, ClaimsIdentity identity)
        {
            var openIddictApplicationSettings = await _openIddictApplicationManager.GetSettingsAsync(openIddictApplication).ConfigureAwait(false);
            var clientClaims = openIddictApplicationSettings?.GetClaimsSettings()?.ClientClaims;

            var clientSubjectClaim = clientClaims?.FirstOrDefault(clientClaim => string.Equals(clientClaim.Key, "client_sub"));

            var clientSubject = clientSubjectClaim?.Value;

            if (string.IsNullOrEmpty(clientSubject))
            {
                var clientId = await _openIddictApplicationManager.GetClientIdAsync(openIddictApplication).ConfigureAwait(false);

                identity.SetClaim(Claims.Subject, clientId);
            }
            else
            {
                var userModel = await GetUserAsync(clientSubject).ConfigureAwait(false);

                if (userModel == null)
                {
                    return Forbid(
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.AccessDenied,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Access denied"
                        }));
                }

                // set claim

                identity.SetClaim(Claims.Subject, userModel.Id);
            }

            return null;
        }

        private async Task<UserModel> GetUserAsync(string subject)
        {
            // Retrieve the profile of the logged in user.

            var user = await _userManager.FindByIdAsync(subject);

            if (user == null)
            {
                return null;
            }

            if (user.Disabled == true ||
                (user.ExpiryDate != null && user.ExpiryDate < DateTimeOffset.Now) ||
                !await _signInManager.CanSignInAsync(user))
            {
                return null;
            }

            return user;
        }

        private IDictionary<string, StringValues> ParseOAuthParameters(HttpContext httpContext, List<string>? excludeKeys = null)
        {
            excludeKeys ??= new List<string>();

            if (httpContext.Request.HasFormContentType)
            {
                return httpContext.Request.Form
                    .Where(key => !excludeKeys.Contains(key.Key))
                    .ToDictionary();
            }
            else
            {
                return httpContext.Request.Query
                    .Where(key => !excludeKeys.Contains(key.Key))
                    .ToDictionary();
            }
        }

        private bool IsAuthenticated(AuthenticateResult authenticateResult, OpenIddictRequest request)
        {
            if (!authenticateResult.Succeeded)
            {
                return false;
            }

            if (request.MaxAge.HasValue && authenticateResult.Properties != null)
            {
                var maxAgeSeconds = TimeSpan.FromSeconds(request.MaxAge.Value);

                var expired = !authenticateResult.Properties.IssuedUtc.HasValue ||
                    DateTimeOffset.UtcNow - authenticateResult.Properties.IssuedUtc > maxAgeSeconds;

                if (expired)
                {
                    return false;
                }
            }

            return true;
        }

        private static string BuildRedirectUrl(HttpRequest request, IDictionary<string, StringValues> queryStringParameters)
        {
            var returnUrl = $"{request.PathBase}{request.Path}{QueryString.Create(queryStringParameters)}";

            var redirectUrl = $"~/Account/Login{QueryString.Create("returnUrl", returnUrl)}";

            return redirectUrl;
        }

        // This has been copied from: https://github.com/openiddict/openiddict-samples/blob/dev/samples/Balosar/Balosar.Server/Controllers/AuthorizationController.cs

        private static IEnumerable<string> GetDestinations(Claim claim)
        {
            // Note: by default, claims are NOT automatically included in the access and identity tokens.
            // To allow OpenIddict to serialize them, you must attach them a destination, that specifies
            // whether they should be included in access tokens, in identity tokens or in both.

            switch (claim.Type)
            {
                case Claims.Name or Claims.PreferredUsername:
                    yield return Destinations.AccessToken;

                    if (claim.Subject!.HasScope(Scopes.Profile))
                        yield return Destinations.IdentityToken;

                    yield break;

                case Claims.Email:
                    yield return Destinations.AccessToken;

                    if (claim.Subject!.HasScope(Scopes.Email))
                        yield return Destinations.IdentityToken;

                    yield break;

                case Claims.Role:
                    yield return Destinations.AccessToken;

                    if (claim.Subject!.HasScope(Scopes.Roles))
                        yield return Destinations.IdentityToken;

                    yield break;

                // Never include the security stamp in the access and identity tokens, as it's a secret value.
                case "AspNet.Identity.SecurityStamp": yield break;

                default:
                    yield return Destinations.AccessToken;
                    yield break;
            }
        }

        private static string GetSubjectId(ClaimsPrincipal claimsPrincipal)
        {
            var id = claimsPrincipal.Identity as ClaimsIdentity;

            var claim = id.FindFirst(JwtClaimTypes.Subject);

            if (claim == null)
            {
                throw new InvalidOperationException("Subject ID claim is missing");
            }

            return claim.Value;
        }
    }
}