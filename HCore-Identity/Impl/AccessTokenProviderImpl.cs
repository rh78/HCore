using HCore.Identity.Database.SqlServer.Models.Impl;
using HCore.Web.API.Impl;
using HCore.Web.Exceptions;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Configuration;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HCore.Identity.Impl
{
    internal class AccessTokenProviderImpl : IAccessTokenProvider
    {
        private readonly UserManager<UserModel> _userManager;
        private readonly IUserClaimsPrincipalFactory<UserModel> _principalFactory;
        private readonly IClientStore _clientStore;
        private readonly IResourceStore _resourceStore;
        private readonly ITokenService _tokenService;
        private readonly IdentityServerOptions _options;
        private readonly IIdentityServicesConfiguration _identityServicesConfiguration;
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
           IIdentityServicesConfiguration identityServicesConfiguration,
           ILogger<AccessTokenProviderImpl> logger)
        {
            _userManager = userManager;            
            _principalFactory = principalFactory;
            _clientStore = clientStore;
            _resourceStore = resourceStore;
            _claimsService = claimsService;
            _tokenService = tokenService;
            _options = options;
            _identityServicesConfiguration = identityServicesConfiguration;
            _logger = logger;
        }

        public async Task<string> GetAccessTokenAsync(string userUuid)
        {
            userUuid = ProcessUserUuid(userUuid);

            try
            {
                var tokenCreationRequest = new TokenCreationRequest();

                var user = await _userManager.FindByIdAsync(userUuid).ConfigureAwait(false);

                if (user == null)
                {
                    throw new NotFoundApiException(NotFoundApiException.UserNotFound, $"User with UUID {userUuid} was not found");
                }

                var identityPricipal = await _principalFactory.CreateAsync(user).ConfigureAwait(false);

                var identityUser = new IdentityServerUser(user.Id.ToString());

                identityUser.AdditionalClaims = identityPricipal.Claims.ToArray();

                identityUser.DisplayName = user.UserName;

                identityUser.AuthenticationTime = DateTime.UtcNow;
                identityUser.IdentityProvider = IdentityServerConstants.LocalIdentityProvider;

                var subject = identityUser.CreatePrincipal();

                tokenCreationRequest.Subject = subject;
                tokenCreationRequest.IncludeAllIdentityClaims = true;

                tokenCreationRequest.ValidatedRequest = new ValidatedRequest();

                tokenCreationRequest.ValidatedRequest.Subject = tokenCreationRequest.Subject;

                string defaultClientId = _identityServicesConfiguration.DefaultClientId;

                var client = await _clientStore.FindClientByIdAsync(defaultClientId).ConfigureAwait(false);

                tokenCreationRequest.ValidatedRequest.SetClient(client);

                var resources = await _resourceStore.GetAllEnabledResourcesAsync().ConfigureAwait(false);

                tokenCreationRequest.Resources = resources;

                tokenCreationRequest.ValidatedRequest.Options = _options;

                tokenCreationRequest.ValidatedRequest.ClientClaims = tokenCreationRequest.ValidatedRequest.ClientClaims.Concat(identityUser.AdditionalClaims).ToList();

                var accessToken = await CreateAccessTokenAsync(tokenCreationRequest).ConfigureAwait(false);

                string defaultClientAuthority = _identityServicesConfiguration.DefaultClientAuthority;
                string defaultClientAudience = _identityServicesConfiguration.DefaultClientAudience;

                accessToken.Issuer = defaultClientAuthority;
                accessToken.Audiences = new string[] { defaultClientAudience };

                var accessTokenValue = await _tokenService.CreateSecurityTokenAsync(accessToken);

                return accessTokenValue;
            }
            catch (ApiException e)
            {
                throw e;
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
                request.Resources,
                request.ValidatedRequest));

            if (request.ValidatedRequest.Client.IncludeJwtId)
            {
                claims.Add(new Claim(JwtClaimTypes.JwtId, CryptoRandom.CreateUniqueId(16)));
            }

            var issuer = _identityServicesConfiguration.DefaultClientAuthority;
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

            foreach (var api in request.Resources.ApiResources)
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
                throw new InvalidArgumentApiException(InvalidArgumentApiException.UserUuidMissing, "The user UUID is missing");

            if (!ApiImpl.Uuid.IsMatch(userUuid))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.UserUuidInvalid, "The user UUID is invalid");

            if (userUuid.Length > UserModel.MaxUserUuidLength)
                throw new InvalidArgumentApiException(InvalidArgumentApiException.UserUuidInvalid, "The user UUID is invalid");

            return userUuid;
        }
    }
}
