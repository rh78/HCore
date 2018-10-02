using IdentityServer4;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Builder
{
    public static class IdentityApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseCoreIdentity(this IApplicationBuilder app)
        {
            var scopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();

            using (var scope = scopeFactory.CreateScope())
            {
                var configurationDbContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                InitializeIdentity(configurationDbContext, configuration);
            }

            app.UseIdentityServer();

            return app;
        }

        private static void InitializeIdentity(ConfigurationDbContext configurationDbContext, IConfiguration configuration)
        {
            string oidcAuthority = configuration[$"Identity:Oidc:Authority"];
            if (string.IsNullOrEmpty(oidcAuthority))
                throw new Exception("Identity OIDC authority string is empty");

            string defaultClientId = configuration[$"Identity:Client:DefaultClientId"];
            if (string.IsNullOrEmpty(defaultClientId))
                throw new Exception("Identity default client ID is empty");

            string defaultClientName = configuration[$"Identity:Client:DefaultClientName"];
            if (string.IsNullOrEmpty(defaultClientName))
                throw new Exception("Identity default client name is empty");

            string defaultClientLogoUrl = configuration[$"Identity:Client:DefaultClientLogoUrl"];
            if (string.IsNullOrEmpty(defaultClientLogoUrl))
                throw new Exception("Identity default client logo URL is empty");

            string defaultClientSecret = configuration[$"Identity:Client:DefaultClientSecret"];
            if (string.IsNullOrEmpty(defaultClientSecret))
                throw new Exception("Identity default client secret is empty");

            string apiResources = configuration["Identity:ApiResources"];
            if (string.IsNullOrEmpty(apiResources))
                throw new Exception("Identity API resources are empty");

            string[] apiResourcesSplit = apiResources.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            if (apiResourcesSplit.Length == 0)
                throw new Exception("Identity API resources are empty");

            List<string> scopes = new List<string>();
            scopes.Add(IdentityServerConstants.StandardScopes.OpenId);
            scopes.Add(IdentityServerConstants.StandardScopes.Profile);
            scopes.Add(IdentityServerConstants.StandardScopes.Email);
            scopes.Add(IdentityServerConstants.StandardScopes.OfflineAccess);
            
            List<IdentityServer4.Models.ApiResource> apiResourcesList = new List<IdentityServer4.Models.ApiResource>();

            for (int i = 0; i < apiResourcesSplit.Length; i++)
            {
                scopes.Add(apiResourcesSplit[i]);

                string apiResourceName = configuration[$"Identity:ApiResourceDetails:{apiResourcesSplit[i]}:Name"];
                if (string.IsNullOrEmpty(apiResourceName))
                    throw new Exception($"Identity API resource name for API resource {apiResourcesSplit[i]} is empty");

                apiResourcesList.Add(new IdentityServer4.Models.ApiResource(apiResourcesSplit[i], apiResourceName));
            }
            
            var defaultClient = new IdentityServer4.Models.Client
            {
                ClientId = defaultClientId,
                ClientName = defaultClientName,
                LogoUri = defaultClientLogoUrl,
                AbsoluteRefreshTokenLifetime = Int32.MaxValue,
                AllowAccessTokensViaBrowser = true,
                AlwaysSendClientClaims = true,
                IncludeJwtId = true,
                RefreshTokenExpiration = TokenExpiration.Sliding,
                AllowedGrantTypes = GrantTypes.Implicit,
                AllowedScopes = scopes,
                AllowOfflineAccess = true,
                RequireConsent = false,
                ClientSecrets =
                {
                    new IdentityServer4.Models.Secret(defaultClientSecret.Sha256())
                },
                RedirectUris =
                {
                    $"{oidcAuthority}signin-oidc"
                },
                PostLogoutRedirectUris =
                {
                    $"{oidcAuthority}",
                    $"{oidcAuthority}signout-callback-oidc"
                }
            };

            if (!configurationDbContext.Clients.Any())
            {
                Console.WriteLine("Clients are being populated...");

                configurationDbContext.Clients.Add(defaultClient.ToEntity());

                configurationDbContext.SaveChanges();

                Console.WriteLine("Clients populated successfully");
            }
            
            if (!configurationDbContext.ApiResources.Any())
            {
                Console.WriteLine("API resources are being populated...");

                foreach (var apiResource in apiResourcesList)
                {
                    configurationDbContext.ApiResources.Add(apiResource.ToEntity());
                }

                configurationDbContext.SaveChanges();

                Console.WriteLine("API resources populated successfully");
            }

            if (!configurationDbContext.IdentityResources.Any())
            {
                Console.WriteLine("Identity resources are being populated...");

                configurationDbContext.IdentityResources.Add(new IdentityResources.OpenId().ToEntity());
                configurationDbContext.IdentityResources.Add(new IdentityResources.Profile().ToEntity());
                configurationDbContext.IdentityResources.Add(new IdentityResources.Email().ToEntity());
                configurationDbContext.IdentityResources.Add(new IdentityResources.Phone().ToEntity());

                configurationDbContext.SaveChanges();

                Console.WriteLine("Identity resources populated successfully");
            }
        }
    }
}