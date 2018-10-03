using IdentityServer4;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Builder
{
    public static class IdentityApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseCoreIdentity(this IApplicationBuilder app)
        {
            app.Validate();

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
            string oidcAudience = configuration[$"Identity:Oidc:Audience"];
            if (string.IsNullOrEmpty(oidcAudience))
                throw new Exception("Identity audience string is empty");

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
            
            List<ApiResource> apiResourcesList = new List<ApiResource>();

            for (int i = 0; i < apiResourcesSplit.Length; i++)
            {
                scopes.Add(apiResourcesSplit[i]);

                string apiResourceName = configuration[$"Identity:ApiResourceDetails:{apiResourcesSplit[i]}:Name"];
                if (string.IsNullOrEmpty(apiResourceName))
                    throw new Exception($"Identity API resource name for API resource {apiResourcesSplit[i]} is empty");

                apiResourcesList.Add(new ApiResource(apiResourcesSplit[i], apiResourceName));
            }
            
            var defaultClient = new Client
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
                    new Secret(defaultClientSecret.Sha256())
                },
                RedirectUris =
                {
                    $"{oidcAudience}signin-oidc"
                },
                PostLogoutRedirectUris =
                {
                    $"{oidcAudience}",
                    $"{oidcAudience}signout-callback-oidc"
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

        internal static void Validate(this IApplicationBuilder app)
        {
            var loggerFactory = app.ApplicationServices.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            var scopeFactory = app.ApplicationServices.GetService<IServiceScopeFactory>();

            using (var scope = scopeFactory.CreateScope())
            {
                var serviceProvider = scope.ServiceProvider;

                ValidateAsync(serviceProvider).GetAwaiter().GetResult();
            }
        }

        private static async Task ValidateAsync(IServiceProvider services)
        {
            var schemes = services.GetRequiredService<IAuthenticationSchemeProvider>();

            if (await schemes.GetDefaultAuthenticateSchemeAsync() == null)
            {
                Console.WriteLine("No authentication scheme has been set");
            }
            else
            {
                string defaultAuthenticationScheme = (await schemes.GetDefaultAuthenticateSchemeAsync())?.Name;
                string defaultSignInScheme = (await schemes.GetDefaultSignInSchemeAsync())?.Name;
                string defaultSignOutScheme = (await schemes.GetDefaultSignOutSchemeAsync())?.Name;
                string defaultChallengeScheme = (await schemes.GetDefaultChallengeSchemeAsync())?.Name;
                string defaultForbidScheme = (await schemes.GetDefaultForbidSchemeAsync())?.Name;

                Console.WriteLine($"Using {defaultAuthenticationScheme} as default ASP.NET Core scheme for authentication");
                Console.WriteLine($"Using {defaultSignInScheme} as default ASP.NET Core scheme for sign-in");
                Console.WriteLine($"Using {defaultSignOutScheme} as default ASP.NET Core scheme for sign-out");
                Console.WriteLine($"Using {defaultChallengeScheme} as default ASP.NET Core scheme for challenge");
                Console.WriteLine($"Using {defaultForbidScheme} as default ASP.NET Core scheme for forbid");
            }
        }
    }
}