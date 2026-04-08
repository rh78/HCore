using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityServer.EntityFramework.DbContexts;
using HCore.Identity;
using HCore.Identity.Database.SqlServer;
using HCore.Identity.Extensions;
using HCore.Identity.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Microsoft.AspNetCore.Builder
{
    public static class IdentityApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseCoreIdentity(this IApplicationBuilder app, bool migrateTenants = true)
        {
            app.Validate();

            var scopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();

            using (var scope = scopeFactory.CreateScope())
            {
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                bool useTenants = configuration.GetValue<bool>("Identity:UseTenants");

                if (useTenants)
                {
                    app.UseTenants(migrateTenants);
                }

                bool useIdentity = configuration.GetValue<bool>("Identity:UseIdentity");

                if (useIdentity)
                {
                    var identityDbContext = scope.ServiceProvider.GetRequiredService<SqlServerIdentityDbContext>();
                    var identityServerConfigurationDbContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

                    identityDbContext.Database.Migrate();

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                    InitializeIdentityAsync(scope, identityDbContext, identityServerConfigurationDbContext, configuration).Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                }
            }

            return app;
        }

        private async static Task InitializeIdentityAsync(IServiceScope scope, SqlServerIdentityDbContext identityDbContext, ConfigurationDbContext identityServerConfigurationDbContext, IConfiguration configuration)
        {
            var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

            var identityServerClients = await identityServerConfigurationDbContext.Clients
                .Include(client => client.Claims)
                .Include(client => client.ClientSecrets)
                .Include(client => client.PostLogoutRedirectUris)
                .Include(client => client.RedirectUris)
                .Include(client => client.AllowedScopes)
                .Include(client => client.AllowedGrantTypes)
                .ToListAsync();

            foreach (var identityServerClient in identityServerClients)
            {
                var existingClient = await manager.FindByClientIdAsync(identityServerClient.ClientId).ConfigureAwait(false);

                if (existingClient != null)
                {
                    continue;
                }

                var openIddictApplicationDescriptor = new OpenIddictApplicationDescriptor()
                {
                    ApplicationType = ApplicationTypes.Web,
                    ClientId = identityServerClient.ClientId,
                    // make sure we mark the secret as a legacy one coming from Identity Server
                    ClientSecret = $"|{identityServerClient.ClientSecrets.First().Value}",
                    ClientType = ClientTypes.Confidential,
                    ConsentType = identityServerClient.RequireConsent ? ConsentTypes.Explicit : ConsentTypes.Implicit,
                    DisplayName = identityServerClient.ClientName
                };

                openIddictApplicationDescriptor.SetAccessTokenLifetime(TimeSpan.FromSeconds(identityServerClient.AccessTokenLifetime));
                openIddictApplicationDescriptor.SetAuthorizationCodeLifetime(TimeSpan.FromSeconds(identityServerClient.AuthorizationCodeLifetime));
                openIddictApplicationDescriptor.SetDeviceCodeLifetime(TimeSpan.FromSeconds(identityServerClient.DeviceCodeLifetime));
                openIddictApplicationDescriptor.SetIdentityTokenLifetime(TimeSpan.FromSeconds(identityServerClient.IdentityTokenLifetime));
                openIddictApplicationDescriptor.SetRefreshTokenLifetime(TimeSpan.FromSeconds(identityServerClient.AbsoluteRefreshTokenLifetime));

                if (identityServerClient.RedirectUris != null)
                {
                    foreach (var redirectUri in identityServerClient.RedirectUris)
                    {
                        var uri = redirectUri.RedirectUri;

                        uri = uri.Replace("*", "WILDCARD");

                        openIddictApplicationDescriptor.RedirectUris.Add(new Uri(uri));
                    }
                }

                if (identityServerClient.PostLogoutRedirectUris != null)
                {
                    foreach (var postLogoutRedirectUri in identityServerClient.PostLogoutRedirectUris)
                    {
                        var uri = postLogoutRedirectUri.PostLogoutRedirectUri;

                        uri = uri.Replace("*", "WILDCARD");

                        openIddictApplicationDescriptor.PostLogoutRedirectUris.Add(new Uri(uri));
                    }
                }

                if (identityServerClient.RequirePkce)
                {
                    openIddictApplicationDescriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);
                }

                if (identityServerClient.RequirePushedAuthorization)
                {
                    openIddictApplicationDescriptor.Requirements.Add(Requirements.Features.PushedAuthorizationRequests);
                }

                if (identityServerClient.AllowedGrantTypes != null)
                {
                    openIddictApplicationDescriptor.AddGrantTypePermissions(identityServerClient.AllowedGrantTypes.Select(allowedGrantType => allowedGrantType.GrantType).ToArray());
                }

                if (identityServerClient.AllowedScopes != null)
                {
                    openIddictApplicationDescriptor.AddScopePermissions(identityServerClient.AllowedScopes.Select(allowedScope => allowedScope.Scope).ToArray());
                }

                if (identityServerClient.Claims != null && identityServerClient.Claims.Any())
                {
                    openIddictApplicationDescriptor.SetClaimsSettings(identityServerClient, isLegacyClientSecret: true);
                }

                await manager.CreateAsync(openIddictApplicationDescriptor).ConfigureAwait(false);
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

                ValidateAsync(serviceProvider).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        private static async Task ValidateAsync(IServiceProvider services)
        {
            var schemes = services.GetRequiredService<IAuthenticationSchemeProvider>();

            if (await schemes.GetDefaultAuthenticateSchemeAsync().ConfigureAwait(false) == null)
            {
                Console.WriteLine("No authentication scheme has been set");
            }
            else
            {
                string defaultAuthenticationScheme = (await schemes.GetDefaultAuthenticateSchemeAsync().ConfigureAwait(false))?.Name;
                string defaultSignInScheme = (await schemes.GetDefaultSignInSchemeAsync().ConfigureAwait(false))?.Name;
                string defaultSignOutScheme = (await schemes.GetDefaultSignOutSchemeAsync().ConfigureAwait(false))?.Name;
                string defaultChallengeScheme = (await schemes.GetDefaultChallengeSchemeAsync().ConfigureAwait(false))?.Name;
                string defaultForbidScheme = (await schemes.GetDefaultForbidSchemeAsync().ConfigureAwait(false))?.Name;

                Console.WriteLine($"Using {defaultAuthenticationScheme} as default ASP.NET Core scheme for authentication");
                Console.WriteLine($"Using {defaultSignInScheme} as default ASP.NET Core scheme for sign-in");
                Console.WriteLine($"Using {defaultSignOutScheme} as default ASP.NET Core scheme for sign-out");
                Console.WriteLine($"Using {defaultChallengeScheme} as default ASP.NET Core scheme for challenge");
                Console.WriteLine($"Using {defaultForbidScheme} as default ASP.NET Core scheme for forbid");
            }
        }
    }
}