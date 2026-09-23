using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HCore.Identity.Database.SqlServer.Models.Impl;
using HCore.Identity.Extensions;
using HCore.Tenants.Models;
using HCore.Tenants.Providers;
using IdentityModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace HCore.Identity.Internal
{
    public class AccessTokenIssuerHandler : IOpenIddictServerHandler<OpenIddictServerEvents.GenerateTokenContext>
    {
        public AccessTokenIssuerHandler(
            IHttpContextAccessor httpContextAccessor,
            ITenantInfoAccessor tenantInfoAccessor,
            IConfiguration configuration)
        {
            HttpContextAccessor = httpContextAccessor;
            TenantInfoAccessor = tenantInfoAccessor;

            DefaultClientAuthority = configuration[$"Identity:DefaultClient:Authority"];

            if (string.IsNullOrEmpty(DefaultClientAuthority))
            {
                throw new Exception("Identity default client authority string is empty");
            }
        }

        protected IHttpContextAccessor HttpContextAccessor { get; }
        protected ITenantInfoAccessor TenantInfoAccessor { get; }

        protected string DefaultClientAuthority { get; }

        public async ValueTask HandleAsync(OpenIddictServerEvents.GenerateTokenContext context)
        {
            var services = HttpContextAccessor.HttpContext.RequestServices;

            var claims = new Dictionary<string, object>();

            var userUuid = context.Principal.GetUserUuid();

            var userModel = await GetUserModelAsync(services, userUuid).ConfigureAwait(false);

            if (userModel != null)
            {
                await AddIdentityPrincipalClaimsAsync(services, userModel, claims).ConfigureAwait(false);
            }

            var openIddictApplicationManager = services.GetRequiredService<IOpenIddictApplicationManager>();

            var openIddictApplication = await openIddictApplicationManager.FindByClientIdAsync(context.ClientId).ConfigureAwait(false);

            await AddClientClaimsAsync(openIddictApplicationManager, openIddictApplication, claims).ConfigureAwait(false);

            await AddScopeClaimAsync(openIddictApplicationManager, openIddictApplication, claims).ConfigureAwait(false);

            var tenantInfo = TenantInfoAccessor.TenantInfo;

            if (tenantInfo != null)
            {
                AddTenantClaims(tenantInfo, claims);
            }

            AddJwtIdClaim(claims);

            ConfigureSecurityTokenDescriptor(tenantInfo, context, claims);
        }

        protected virtual async Task<UserModel> GetUserModelAsync(IServiceProvider services, string userUuid)
        {
            if (string.IsNullOrEmpty(userUuid))
            {
                return null;
            }

            var userManager = services.GetRequiredService<UserManager<UserModel>>();

            var userModel = await userManager.FindByIdAsync(userUuid).ConfigureAwait(false);

            return userModel;
        }

        protected virtual async Task AddIdentityPrincipalClaimsAsync(IServiceProvider services, UserModel userModel, Dictionary<string, object> claims)
        {
            var principalFactory = services.GetRequiredService<IUserClaimsPrincipalFactory<UserModel>>();

            var identityPrincipal = await principalFactory.CreateAsync(userModel).ConfigureAwait(false);

            var developerAdminClaimValues = identityPrincipal.GetClaims(IdentityCoreConstants.DeveloperAdminClaim);

            if (developerAdminClaimValues != null && developerAdminClaimValues.Any())
            {
                claims[IdentityCoreConstants.DeveloperAdminClaim] = developerAdminClaimValues.ToArray();
            }

            var apiDocsClaimValues = identityPrincipal.GetClaims("api_docs");

            if (apiDocsClaimValues != null && apiDocsClaimValues.Any())
            {
                claims["api_docs"] = apiDocsClaimValues.ToArray();
            }

            var isAdcuClaimValues = identityPrincipal.GetClaims("is_adcu");

            if (isAdcuClaimValues != null && isAdcuClaimValues.Any())
            {
                claims["is_adcu"] = isAdcuClaimValues.ToArray();
            }
        }

        protected virtual async Task AddClientClaimsAsync(IOpenIddictApplicationManager openIddictApplicationManager, object openIddictApplication, Dictionary<string, object> claims)
        {
            var openIddictApplicationSettings = await openIddictApplicationManager.GetSettingsAsync(openIddictApplication).ConfigureAwait(false);
            var clientClaims = openIddictApplicationSettings?.GetClaimsSettings()?.ClientClaims;

            if (clientClaims != null && clientClaims.Any())
            {
                foreach (var clientClaimKeyValuePair in clientClaims)
                {
                    claims[clientClaimKeyValuePair.Key] = clientClaimKeyValuePair.Value;
                }
            }
        }

        protected virtual async Task AddScopeClaimAsync(IOpenIddictApplicationManager openIddictApplicationManager, object openIddictApplication, Dictionary<string, object> claims)
        {
            var openIddictApplicationPermissions = await openIddictApplicationManager.GetPermissionsAsync(openIddictApplication).ConfigureAwait(false);

            if (openIddictApplicationPermissions != null)
            {
                claims[JwtClaimTypes.Scope] = openIddictApplicationPermissions
                    .Where(openIddictApplicationPermission => openIddictApplicationPermission.StartsWith(Permissions.Prefixes.Scope))
                    .Select(openIddictApplicationPermission => openIddictApplicationPermission[Permissions.Prefixes.Scope.Length..])
                    .Where(openIddictApplicationPermission => !string.IsNullOrEmpty(openIddictApplicationPermission))
                    .ToArray();
            }
        }

        protected virtual void AddTenantClaims(ITenantInfo tenantInfo, Dictionary<string, object> claims)
        {
            claims[IdentityCoreConstants.DeveloperUuidClientClaim] = tenantInfo.DeveloperUuid;
            claims[IdentityCoreConstants.TenantUuidClientClaim] = tenantInfo.TenantUuid;
        }

        protected virtual void AddJwtIdClaim(Dictionary<string, object> claims)
        {
            claims[JwtClaimTypes.JwtId] = CryptoRandom.CreateUniqueId(16);
        }

        protected virtual void ConfigureSecurityTokenDescriptor(ITenantInfo tenantInfo, OpenIddictServerEvents.GenerateTokenContext context, Dictionary<string, object> claims)
        {
            context.SecurityTokenDescriptor.Claims = claims;

            if (context.TokenType == TokenTypeIdentifiers.AccessToken)
            {
                context.SecurityTokenDescriptor.Issuer = DefaultClientAuthority;
            }

            if (tenantInfo != null)
            {
                context.SecurityTokenDescriptor.Audience = tenantInfo.PortalsBackendApiUrl;
            }

            context.SecurityTokenDescriptor.NotBefore = context.SecurityTokenDescriptor.IssuedAt;
        }
    }
}
