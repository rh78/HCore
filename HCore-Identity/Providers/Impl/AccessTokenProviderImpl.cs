using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HCore.Identity.Database.SqlServer.Models.Impl;
using HCore.Identity.Extensions;
using HCore.Web.API.Impl;
using HCore.Web.Exceptions;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace HCore.Identity.Providers.Impl
{
    public class AccessTokenProviderImpl : IAccessTokenProvider
    {
        private readonly UserManager<UserModel> _userManager;
        private readonly IUserClaimsPrincipalFactory<UserModel> _principalFactory;
        private readonly IConfigurationProvider _configurationProvider;
        
        private readonly IOpenIddictApplicationManager _openIddictApplicationManager;
        private readonly IOptionsMonitor<OpenIddictServerOptions> _openIddictServerOptions;
        
        private readonly ILogger<AccessTokenProviderImpl> _logger;

        public AccessTokenProviderImpl(
           UserManager<UserModel> userManager,
           IUserClaimsPrincipalFactory<UserModel> principalFactory,
           IConfigurationProvider configurationProvider,
           IOpenIddictApplicationManager openIddictApplicationManager,
           IOptionsMonitor<OpenIddictServerOptions> openIddictServerOptions,
           ILogger<AccessTokenProviderImpl> logger)
        {
            _userManager = userManager;
            _principalFactory = principalFactory;
            _configurationProvider = configurationProvider;

            _openIddictApplicationManager = openIddictApplicationManager;
            _openIddictServerOptions = openIddictServerOptions;

            _logger = logger;
        }

        public async Task<string> GetAccessTokenAsync(string userUuid, List<Claim> additionalClaims = null, string userUuidOverride = null)
        {
            userUuid = ProcessUserUuid(userUuid);

            try
            {
                var user = await _userManager.FindByIdAsync(userUuid).ConfigureAwait(false);

                if (user == null)
                {
                    throw new NotFoundApiException(NotFoundApiException.UserNotFound, $"User with UUID {userUuid} was not found", userUuid);
                }

                return await GetAccessTokenAsync(user, additionalClaims, userUuidOverride).ConfigureAwait(false);
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error when getting access token: {e}");

                throw new InternalServerErrorApiException();
            }
        }

        public async Task<string> GetAccessTokenAsync(UserModel user, List<Claim> additionalClaims = null, string userUuidOverride = null)
        { 
            try
            {
                var openIddictServerOptions = _openIddictServerOptions.CurrentValue;

                string defaultClientId = _configurationProvider.DefaultClientId;

                var openIddictApplication = await _openIddictApplicationManager.FindByClientIdAsync(defaultClientId).ConfigureAwait(false);

                // build claims

                var userUuid = !string.IsNullOrEmpty(userUuidOverride) ? userUuidOverride : user.Id.ToString();

                var claims = new Dictionary<string, object>
                {
                    { Claims.Subject, userUuid },
                    { "client_id", defaultClientId },
                    { "idp", "local" }
                };

                // add claims from identity principal

                var identityPricipal = await _principalFactory.CreateAsync(user).ConfigureAwait(false);

                var developerAdminClaimValues = identityPricipal.GetClaims(IdentityCoreConstants.DeveloperAdminClaim);

                if (developerAdminClaimValues != null && developerAdminClaimValues.Any())
                {
                    claims[IdentityCoreConstants.DeveloperAdminClaim] = developerAdminClaimValues.ToArray();
                }

                var apiDocsClaimValues = identityPricipal.GetClaims("api_docs");

                if (apiDocsClaimValues != null && apiDocsClaimValues.Any())
                {
                    claims["api_docs"] = apiDocsClaimValues.ToArray();
                }

                var isAdcuClaimValues = identityPricipal.GetClaims("is_adcu");

                if (isAdcuClaimValues != null && isAdcuClaimValues.Any())
                {
                    claims["is_adcu"] = isAdcuClaimValues.ToArray();
                }

                // add client claims

                var openIddictApplicationSettings = await _openIddictApplicationManager.GetSettingsAsync(openIddictApplication).ConfigureAwait(false);
                var clientClaims = openIddictApplicationSettings?.GetClaimsSettings()?.ClientClaims;

                if (clientClaims != null && clientClaims.Any())
                {
                    foreach (var clientClaimKeyValuePair in clientClaims)
                    {
                        claims[clientClaimKeyValuePair.Key] = clientClaimKeyValuePair.Value;
                    }
                }

                // add additional client claims

                if (additionalClaims != null)
                {
                    foreach (Claim additionalClaim in additionalClaims)
                    {
                        if (!claims.Any(claim => string.Equals(claim.Key, additionalClaim.Type)))
                        {
                            claims.Add(additionalClaim.Type, additionalClaim.Value);
                        }
                    }
                }

                // add scopes

                var openIddictApplicationPermissions = await _openIddictApplicationManager.GetPermissionsAsync(openIddictApplication).ConfigureAwait(false);

                if (openIddictApplicationPermissions != null)
                {
                    claims[JwtClaimTypes.Scope] = openIddictApplicationPermissions
                        .Where(openIddictApplicationPermission => openIddictApplicationPermission.StartsWith(Permissions.Prefixes.Scope))
                        .Select(openIddictApplicationPermission => openIddictApplicationPermission[Permissions.Prefixes.Scope.Length..])
                        .Where(openIddictApplicationPermission => !string.IsNullOrEmpty(openIddictApplicationPermission))
                        .ToArray(); 
                }
                
                claims[JwtClaimTypes.JwtId] = CryptoRandom.CreateUniqueId(16);

                var issuer = _configurationProvider.DefaultClientAuthority;

                var issuedAt = DateTime.UtcNow;
                var expiresAt = issuedAt.AddHours(1);

                var tokenDescriptor = new SecurityTokenDescriptor()
                {
                    Claims = claims,
                    IssuedAt = issuedAt,
                    NotBefore = issuedAt,
                    Expires = expiresAt,
                    Issuer = issuer,
                    Audience = _configurationProvider.DefaultClientAudience,
                    TokenType = TokenTypes.Bearer,
                    SigningCredentials = openIddictServerOptions.SigningCredentials.FirstOrDefault(),
                    EncryptingCredentials = null
                };
                
                var accessTokenValue = openIddictServerOptions.JsonWebTokenHandler.CreateToken(tokenDescriptor);

                return accessTokenValue;
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error when getting access token: {e}");

                throw new InternalServerErrorApiException();
            }
        }

        private string ProcessUserUuid(string userUuid)
        {
            if (string.IsNullOrEmpty(userUuid))
                throw new RequestFailedApiException(RequestFailedApiException.UserUuidMissing, "The user UUID is missing");

            if (!ApiImpl.Uuid.IsMatch(userUuid))
                throw new RequestFailedApiException(RequestFailedApiException.UserUuidInvalid, "The user UUID is invalid");

            if (userUuid.Length > ApiImpl.MaxUserUuidLength)
                throw new RequestFailedApiException(RequestFailedApiException.UserUuidTooLong, "The user UUID is too long");

            return userUuid;
        }
    }
}
