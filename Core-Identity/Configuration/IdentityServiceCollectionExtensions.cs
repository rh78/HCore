using AspNetCore.DataProtection.SqlServer;
using IdentityServer4;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ReinhardHolzner.Core.Identity.Database.SqlServer;
using ReinhardHolzner.Core.Identity.Database.SqlServer.Models.Impl;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IdentityServiceCollectionExtensions
    {
        public static IServiceCollection AddCoreIdentity<TStartup>(this IServiceCollection services, IConfiguration configuration)
        {
            var migrationsAssembly = typeof(TStartup).GetTypeInfo().Assembly.GetName().Name;

            string connectionString = configuration[$"SqlServer:Identity:ConnectionString"];
            if (string.IsNullOrEmpty(connectionString))
                throw new Exception("SQL Server connection string is empty");

            string oidcAuthority = configuration[$"Identity:Oidc:Authority"];
            if (string.IsNullOrEmpty(oidcAuthority))
                throw new Exception("Identity OIDC authority string is empty");

            string applicationName = configuration[$"Identity:Application:Name"];
            if (string.IsNullOrEmpty(applicationName))
                throw new Exception("Identity application name is empty");

            string cookieDomain = configuration[$"Identity:Cookie:Domain"];
            if (string.IsNullOrEmpty(cookieDomain))
                throw new Exception("Identity cookie domain is empty");

            string cookieName = configuration[$"Identity:Cookie:Name"];
            if (string.IsNullOrEmpty(cookieName))
                throw new Exception("Identity cookie name is empty");

            string defaultClientId = configuration[$"Identity:Client:DefaultClientId"];
            if (string.IsNullOrEmpty(defaultClientId))
                throw new Exception("Identity default client ID is empty");

            string defaultClientSecret = configuration[$"Identity:Client:DefaultClientSecret"];
            if (string.IsNullOrEmpty(defaultClientSecret))
                throw new Exception("Identity default client secret is empty");

            string apiResources = configuration["Identity:ApiResources"];
            if (string.IsNullOrEmpty(apiResources))
                throw new Exception("Identity API resources are empty");

            string[] apiResourcesSplit = apiResources.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            if (apiResourcesSplit.Length == 0)
                throw new Exception("Identity API resources are empty");
            
            services.AddSqlServer<TStartup, SqlServerIdentityDbContext>("Identity", configuration);

            // lightweight one for default UI implementations
            services.AddSqlServer<TStartup, IdentityDbContext>("Identity", configuration);

            services.AddDataProtection()
                .PersistKeysToSqlServer(connectionString)
                .SetApplicationName(applicationName);

            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Domain = cookieDomain;
                options.Cookie.Name = cookieName;                
            });            

            services.Configure<IdentityOptions>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            });

            var identityBuilder = services.AddIdentity<UserModel, IdentityRole>();

            identityBuilder.AddEntityFrameworkStores<SqlServerIdentityDbContext>();
            identityBuilder.AddDefaultTokenProviders();

            var identityServerBuilder = services.AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;
                options.UserInteraction.ErrorUrl = "/Account/Error";
            });

            // see http://amilspage.com/signing-certificates-idsv4/

            identityServerBuilder.AddSigningCredential(GetSigningKeyCertificate(configuration));

            identityServerBuilder.AddAspNetIdentity<UserModel>();

            // this adds the config data from DB (clients, resources)
            identityServerBuilder.AddConfigurationStore(options =>
            {
                options.ConfigureDbContext = dbContextBuilder =>
                    dbContextBuilder.UseSqlServer(connectionString,
                        sqlServerOptions => sqlServerOptions.MigrationsAssembly(migrationsAssembly));
            });

            // this adds the operational data from DB (codes, tokens, consents)
            identityServerBuilder.AddOperationalStore(options =>
            {
                options.ConfigureDbContext = dbContextBuilder =>
                    dbContextBuilder.UseSqlServer(connectionString,
                        sqlServerOptions => sqlServerOptions.MigrationsAssembly(migrationsAssembly));

                // this enables automatic token cleanup. this is optional.
                options.EnableTokenCleanup = true;
                // options.TokenCleanupInterval = 15; // frequency in seconds to cleanup stale grants. 15 is useful during debugging
            });

            var authenticationBuilder = services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "oidc";
            });

            bool useJwt = configuration.GetValue<bool>("WebServer:UseJwt");

            if (useJwt)
            {
                JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

                authenticationBuilder.AddJwtBearer("Jwt", jwt =>
                {
                    jwt.Authority = oidcAuthority;
                    jwt.RequireHttpsMetadata = true;
                    jwt.Audience = oidcAuthority;
                });
            }            

            authenticationBuilder.AddCookie("Cookies", cookie =>
            {
                cookie.Cookie.Domain = cookieDomain;
                cookie.Cookie.Name = cookieName;
            });

            authenticationBuilder.AddOpenIdConnect("oidc", oidc =>
            {
                oidc.SignInScheme = "Cookies";

                oidc.Authority = oidcAuthority;
                
                oidc.ClientId = defaultClientId;
                oidc.ClientSecret = defaultClientSecret;
                oidc.ResponseType = "id_token token";

                oidc.SaveTokens = true;
                oidc.GetClaimsFromUserInfoEndpoint = true;

                oidc.Scope.Add(IdentityServerConstants.StandardScopes.OpenId);
                oidc.Scope.Add(IdentityServerConstants.StandardScopes.Profile);
                oidc.Scope.Add(IdentityServerConstants.StandardScopes.Email);
                oidc.Scope.Add(IdentityServerConstants.StandardScopes.OfflineAccess);

                for (int i = 0; i < apiResourcesSplit.Length; i++)
                {
                    oidc.Scope.Add(apiResourcesSplit[i]);
                }
            });

            services.AddSingleton<IEmailSender, ReinhardHolzner.Core.Identity.EmailSender.Impl.EmailSenderImpl>();

            return services;
        }        

        private static X509Certificate2 GetSigningKeyCertificate(IConfiguration configuration)
        {
            string signingKeyAssembly = configuration["Identity:SigningKey:Assembly"];
            if (string.IsNullOrEmpty(signingKeyAssembly))
                throw new Exception("Identity signing assembly not found");

            string signingKeyName = configuration["Identity:SigningKey:Name"];
            if (string.IsNullOrEmpty(signingKeyName))
                throw new Exception("Identity signing key name not found");

            string signingKeyPassword = configuration["Identity:SigningKey:Password"];
            if (string.IsNullOrEmpty(signingKeyPassword))
                throw new Exception("Identity signing password not found");

            Assembly _signingKeyAssembly = AppDomain.CurrentDomain.GetAssemblies().
                SingleOrDefault(assembly => assembly.GetName().Name == signingKeyAssembly);

            if (signingKeyAssembly == null)
                throw new Exception("Identity signing key assembly is not present in the list of assemblies");

            var resourceStream = _signingKeyAssembly.GetManifestResourceStream(signingKeyName);

            if (resourceStream == null)
                throw new Exception("Identity core signing key resource not found");

            using (var memory = new MemoryStream((int)resourceStream.Length))
            {
                resourceStream.CopyTo(memory);

                return new X509Certificate2(memory.ToArray(), signingKeyPassword);
            }
        }
    }
}
