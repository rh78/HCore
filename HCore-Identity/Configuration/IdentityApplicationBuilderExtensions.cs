using System;
using System.Threading.Tasks;
using HCore.Identity.Database.SqlServer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

                    identityDbContext.Database.Migrate();
                }
            }

            return app;
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