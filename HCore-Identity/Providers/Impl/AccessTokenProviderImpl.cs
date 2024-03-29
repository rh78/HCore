﻿using HCore.Identity.Database.SqlServer.Models.Impl;
using HCore.Web.API.Impl;
using HCore.Web.Exceptions;
using IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HCore.Identity.Providers.Impl
{
    public class AccessTokenProviderImpl : IAccessTokenProvider
    {
        private readonly UserManager<UserModel> _userManager;
        private readonly IUserClaimsPrincipalFactory<UserModel> _principalFactory;
        private readonly IClientStore _clientStore;
        private readonly IResourceStore _resourceStore;
        private readonly ITokenService _tokenService;
        private readonly IdentityServerOptions _options;
        private readonly IConfigurationProvider _configurationProvider;
        private readonly IClaimsService _claimsService;
        private readonly ILogger<AccessTokenProviderImpl> _logger;

        public AccessTokenProviderImpl(
           UserManager<UserModel> userManager,
           IUserClaimsPrincipalFactory<UserModel> principalFactory,
           IClientStore clientStore,
           IResourceStore resourceStore,
           IClaimsService claimsService,
           ITokenService tokenService,
           IdentityServerOptions options,
           IConfigurationProvider configurationProvider,
           ILogger<AccessTokenProviderImpl> logger)
        {
            _userManager = userManager;            
            _principalFactory = principalFactory;
            _clientStore = clientStore;
            _resourceStore = resourceStore;
            _claimsService = claimsService;
            _tokenService = tokenService;
            _options = options;
            _configurationProvider = configurationProvider;
            _logger = logger;
        }

        public async Task<string> GetAccessTokenAsync(string userUuid, List<Claim> additionalClientClaims = null, string userUuidOverride = null)
        {
            userUuid = ProcessUserUuid(userUuid);

            try
            {
                var user = await _userManager.FindByIdAsync(userUuid).ConfigureAwait(false);

                if (user == null)
                {
                    throw new NotFoundApiException(NotFoundApiException.UserNotFound, $"User with UUID {userUuid} was not found", userUuid);
                }

                return await GetAccessTokenAsync(user, additionalClientClaims, userUuidOverride).ConfigureAwait(false);
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

        public async Task<string> GetAccessTokenAsync(UserModel user, List<Claim> additionalClientClaims = null, string userUuidOverride = null)
        { 
            try
            {
                var tokenCreationRequest = new TokenCreationRequest();

                var identityPricipal = await _principalFactory.CreateAsync(user).ConfigureAwait(false);

                var identityUser = new IdentityServerUser(!string.IsNullOrEmpty(userUuidOverride) ? userUuidOverride : user.Id.ToString());

                identityUser.AdditionalClaims = identityPricipal.Claims.ToArray();

                identityUser.DisplayName = user.UserName;

                identityUser.AuthenticationTime = DateTime.UtcNow;
                identityUser.IdentityProvider = IdentityServerConstants.LocalIdentityProvider;

                var subject = identityUser.CreatePrincipal();

                tokenCreationRequest.Subject = subject;

                tokenCreationRequest.IncludeAllIdentityClaims = true;

                tokenCreationRequest.ValidatedRequest = new ValidatedRequest();

                tokenCreationRequest.ValidatedRequest.Subject = tokenCreationRequest.Subject;

                string defaultClientId = _configurationProvider.DefaultClientId;

                var client = await _clientStore.FindClientByIdAsync(defaultClientId).ConfigureAwait(false);

                tokenCreationRequest.ValidatedRequest.SetClient(client);

                var resources = await _resourceStore.GetAllEnabledResourcesAsync().ConfigureAwait(false);

                tokenCreationRequest.ValidatedResources = new ResourceValidationResult(resources);

                tokenCreationRequest.ValidatedRequest.Options = _options;

                if (additionalClientClaims != null)
                {
                    var clientClaims = tokenCreationRequest.ValidatedRequest.ClientClaims;

                    foreach (Claim additionalClientClaim in additionalClientClaims) 
                    {
                        if (!clientClaims.Any(clientClaim => string.Equals(clientClaim.Type, additionalClientClaim.Type)))
                        {
                            clientClaims.Add(additionalClientClaim);
                        }
                    }
                }

                var accessToken = await CreateAccessTokenAsync(tokenCreationRequest).ConfigureAwait(false);

                string defaultClientAuthority = _configurationProvider.DefaultClientAuthority;
                
                accessToken.Issuer = defaultClientAuthority;
                accessToken.Audiences = new string[] { _configurationProvider.DefaultClientAudience };

                var accessTokenValue = await _tokenService.CreateSecurityTokenAsync(accessToken).ConfigureAwait(false);

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

        // see https://github.com/IdentityServer/IdentityServer4/blob/dev/src/Services/Default/DefaultTokenService.cs

        private const string AccessTokenAudience = "{0}resources";

        private async Task<Token> CreateAccessTokenAsync(TokenCreationRequest request)
        {
            var claims = new List<Claim>();
            claims.AddRange(await _claimsService.GetAccessTokenClaimsAsync(
                request.Subject,
                request.ValidatedResources,
                request.ValidatedRequest).ConfigureAwait(false));

            if (request.ValidatedRequest.Client.IncludeJwtId)
            {
                claims.Add(new Claim(JwtClaimTypes.JwtId, CryptoRandom.CreateUniqueId(16)));
            }

            var issuer = _configurationProvider.DefaultClientAuthority;
            var token = new Token(OidcConstants.TokenTypes.AccessToken)
            {
                CreationTime = DateTimeOffset.Now.UtcDateTime,
                Audiences = { string.Format(AccessTokenAudience, issuer) },
                Issuer = issuer,
                Lifetime = request.ValidatedRequest.AccessTokenLifetime,
                Claims = claims,
                ClientId = request.ValidatedRequest.Client.ClientId,
                AccessTokenType = request.ValidatedRequest.AccessTokenType
            };

            foreach (var api in request.ValidatedResources.Resources.ApiResources)
            {
                if (!string.IsNullOrEmpty(api.Name))
                {
                    token.Audiences.Add(api.Name);
                }
            }

            return token;
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
