﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using HCore.Identity;
using HCore.Identity.Database.SqlServer;
using HCore.Identity.Database.SqlServer.Models.Impl;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using HCore.Identity.Requirements;
using IdentityModel;
using System.Security.Claims;
using HCore.Identity.Providers;
using HCore.Identity.Providers.Impl;
using HCore.Identity.Services.Impl;
using HCore.Identity.Services;
using HCore.Identity.Validators.Impl;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using HCore.Tenants;
using IdentityServer4;
using Microsoft.AspNetCore.Authentication;
using Sustainsys.Saml2.AspNetCore2;
using Sustainsys.Saml2;
using Sustainsys.Saml2.Metadata;
using Microsoft.AspNetCore.Http;
using reCAPTCHA.AspNetCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using IdentityServer4.EntityFramework.DbContexts;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using HCore.Identity.Internal;
using Sustainsys.Saml2.Saml2P;
using HCore.Translations.Providers;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IdentityServiceCollectionExtensions
    {
        private const string TriggerAcrValuesGetParameterKey = "AcrTrigger";

        public static IServiceCollection AddCoreIdentity<TStartup>(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IStringLocalizerProvider, MessagesStringLocalizerProviderImpl>();

            var migrationsAssembly = typeof(TStartup).GetTypeInfo().Assembly.GetName().Name;

            bool useIdentity = configuration.GetValue<bool>("Identity:UseIdentity");
            bool useTenants = configuration.GetValue<bool>("Identity:UseTenants");

            TenantsBuilder tenantsBuilder = null;

            if (useTenants)
            {
                tenantsBuilder = services.AddTenants<TStartup>(configuration);
            }

            if (useIdentity)
            {
                ConfigureSqlServer<TStartup>(services, configuration);
                ConfigureDataProtection(services, configuration);

                ConfigureAspNetIdentity(services, tenantsBuilder, configuration);

                ConfigureIdentityServer(services, tenantsBuilder, migrationsAssembly, configuration);
            }

            bool useJwt = configuration.GetValue<bool>("Identity:UseJwt");

            if (useJwt)
            {
                // will also configure external authentication services

                ConfigureJwtAuthentication(services, tenantsBuilder, configuration);
            }
            else
            {
                ConfigureExternalAuthenticationServices(services, tenantsBuilder, configuration);
            }

            // see https://github.com/IdentityServer/IdentityServer4.Samples/blob/release/Quickstarts/Combined_AspNetIdentity_and_EntityFrameworkStorage/src/IdentityServerWithAspIdAndEF/Startup.cs#L84

            services.Configure<IISOptions>(iis =>
            {
                iis.AuthenticationDisplayName = "Windows";
                iis.AutomaticAuthentication = false;
            });

            if (useIdentity)
            {
                services.AddSingleton<IEmailSender, HCore.Identity.EmailSender.Impl.EmailSenderImpl>();

                services.AddSingleton<HCore.Identity.Providers.IConfigurationProvider, ConfigurationProviderImpl>();

                services.AddScoped<IAccessTokenProvider, AccessTokenProviderImpl>();
                services.AddScoped<IIdentityServices, IdentityServicesImpl>();                
            }

            if (useIdentity || useJwt)
            {
                services.AddScoped<IAuthServices, AuthServicesImpl>();
            }

            bool useRecaptcha = configuration.GetValue<bool>("Identity:UseRecaptcha");

            if (useRecaptcha)
            {
                services.Configure<RecaptchaSettings>(configuration.GetSection("Identity:Recaptcha"));

                services.AddTransient<IRecaptchaService, RecaptchaService>();
            }

            return services;
        }

        private static void ConfigureSqlServer<TStartup>(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSqlDatabase<TStartup, SqlServerIdentityDbContext>("Identity", configuration);

            // lightweight one for default UI implementations
            services.AddSqlDatabase<TStartup, IdentityDbContext>("Identity", configuration);
        }

        private static void ConfigureDataProtection(IServiceCollection services, IConfiguration configuration)
        {
            string applicationName = configuration[$"Identity:Application:Name"];
            if (string.IsNullOrEmpty(applicationName))
                throw new Exception("Identity application name is empty");

            services.AddDataProtection()
                .PersistKeysToSqlDatabase()
                .SetApplicationName(applicationName);
        }

        private static void ConfigureJwtAuthentication(IServiceCollection services, TenantsBuilder tenantsBuilder, IConfiguration configuration)
        {
            var authenticationBuilder = services.AddAuthentication();
            
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            string[] requiredScopesSplit = null;

            string requiredScopes = configuration["Identity:Jwt:RequiredScopes"];
            if (!string.IsNullOrEmpty(requiredScopes))
            {
                requiredScopesSplit = requiredScopes.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                if (requiredScopesSplit.Length == 0)
                    requiredScopesSplit = null;
            }

            if (tenantsBuilder == null)
            {
                string defaultClientAuthority = configuration[$"Identity:DefaultClient:Authority"];
                if (string.IsNullOrEmpty(defaultClientAuthority))
                    throw new Exception("Identity default client authority string is empty");

                string defaultClientAudience = configuration[$"Identity:DefaultClient:Audience"];
                if (string.IsNullOrEmpty(defaultClientAudience))
                    throw new Exception("Identity default client audience string is empty");

                authenticationBuilder.AddJwtBearer(IdentityCoreConstants.JwtScheme, jwt =>
                {                       
                    jwt.RequireHttpsMetadata = true;

                    jwt.Authority = defaultClientAuthority;
                    jwt.Audience = defaultClientAudience;

                    jwt.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ClockSkew = TimeSpan.FromMinutes(5),                           
                        RequireSignedTokens = true,
                        RequireExpirationTime = true,
                        ValidateLifetime = true,
                        // audience validation will be done via scope, as recommended in 
                        // https://github.com/IdentityServer/IdentityServer4/issues/127
                        ValidateAudience = false,
                        ValidateIssuer = true,
                        ValidIssuer = defaultClientAuthority
                    };
                });

                services.AddAuthorization(options =>
                {
                    options.AddPolicy(IdentityCoreConstants.JwtPolicy, policy =>
                    {
                        policy.AuthenticationSchemes.Add(IdentityCoreConstants.JwtScheme);
                        
                        if (requiredScopesSplit != null)
                        {
                            policy.RequireAssertion(handler =>
                            {
                                return CheckScopes(handler.User, requiredScopesSplit);
                            });
                        }

                        policy.RequireAuthenticatedUser();
                    });
                });
            } else
            {
                authenticationBuilder.AddJwtBearer(IdentityCoreConstants.JwtScheme, jwt =>
                {
                    jwt.RequireHttpsMetadata = true;                                                
                });

                tenantsBuilder.WithPerTenantOptions<JwtBearerOptions>((jwt, tenantInfo, name) =>
                {
                    jwt.Authority = tenantInfo.DeveloperAuthority;
                    jwt.Audience = tenantInfo.DeveloperAudience;

                    var tokenValidationParameters = new TokenValidationParameters()
                    {
                        ClockSkew = TimeSpan.FromMinutes(5),
                        RequireSignedTokens = true,
                        RequireExpirationTime = true,
                        ValidateLifetime = true,
                        // audience validation will be done via scope, as recommended in 
                        // https://github.com/IdentityServer/IdentityServer4/issues/127
                        ValidateAudience = false,
                        ValidateIssuer = true,
                        ValidIssuer = tenantInfo.DeveloperAuthority
                    };

                    var developerCertificate = tenantInfo.GetDeveloperCertificate();

                    if (developerCertificate != null)
                    {
                        // if we cannot resolve it from some discovery endpoint

                        tokenValidationParameters.IssuerSigningKey = new X509SecurityKey(developerCertificate);
                    }

                    jwt.TokenValidationParameters = tokenValidationParameters;
                });

                ConfigureExternalAuthenticationServices(authenticationBuilder, tenantsBuilder, configuration);

                services.AddAuthorization(options =>
                {
                    options.AddPolicy(IdentityCoreConstants.JwtPolicy, policy =>
                    {
                        policy.AuthenticationSchemes.Add(IdentityCoreConstants.JwtScheme);

                        policy.Requirements.Add(new ClientDeveloperUuidRequirement());

                        if (requiredScopesSplit != null)
                        {
                            policy.RequireAssertion(handler =>
                            {
                                return CheckScopes(handler.User, requiredScopesSplit);
                            });
                        }

                        policy.RequireAuthenticatedUser();
                    });
                });

                services.AddSingleton<IAuthorizationHandler, ClientDeveloperUuidRequirementHandler>();                    
            }             
        }

        private static void ConfigureExternalAuthenticationServices(IServiceCollection services, TenantsBuilder tenantsBuilder, IConfiguration configuration)
        {
            var authenticationBuilder = services.AddAuthentication();

            if (tenantsBuilder != null)
            {
                ConfigureExternalAuthenticationServices(authenticationBuilder, tenantsBuilder, configuration);
            }
        }

        private static void ConfigureExternalAuthenticationServices(AuthenticationBuilder authenticationBuilder, TenantsBuilder tenantsBuilder, IConfiguration configuration)
        {
            if (tenantsBuilder != null)
            {
                authenticationBuilder.AddOpenIdConnect(IdentityCoreConstants.ExternalOidcScheme, openIdConnect =>
                {
                    // will be configured dynamically

                    openIdConnect.ClientId = "N/A";
                    openIdConnect.Authority = "https://nowhere.nowhere";
                });

                tenantsBuilder.WithPerTenantOptions<OpenIdConnectOptions>((openIdConnect, tenantInfo, name) =>
                {
                    if (string.Equals(tenantInfo.ExternalAuthenticationMethod, TenantConstants.ExternalAuthenticationMethodOidc))
                    {
                        var oidcClientId = tenantInfo.OidcClientId;
                        var oidcUsePkce = tenantInfo.OidcUsePkce;
                        var oidcClientSecret = tenantInfo.OidcClientSecret;
                        var oidcEndpointUrl = tenantInfo.OidcEndpointUrl;
                        var oidcScopes = tenantInfo.OidcScopes;
                        var oidcAcrValues = tenantInfo.OidcAcrValues;
                        var oidcAcrValuesAppendix = tenantInfo.OidcAcrValuesAppendix;
                        var oidcTriggerAcrValuesAppendixByUrlParameter = tenantInfo.OidcTriggerAcrValuesAppendixByUrlParameter;

                        openIdConnect.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                        openIdConnect.SignOutScheme = IdentityServerConstants.SignoutScheme;
                        
                        openIdConnect.Authority = oidcEndpointUrl;

                        openIdConnect.ClientId = oidcClientId;

                        if (!oidcUsePkce)
                        {
                            openIdConnect.ClientSecret = oidcClientSecret;
                        }
                        else
                        {
                            openIdConnect.ResponseType = OpenIdConnectResponseType.Code;

                            openIdConnect.UsePkce = true;
                        }

                        if (!string.IsNullOrEmpty(oidcAcrValues))
                        {
                            openIdConnect.Events = new OpenIdConnectEvents
                            {
                                OnRedirectToIdentityProvider = context =>
                                {
                                    context.ProtocolMessage.SetParameter("acr_values", AdjustAcrValues(oidcAcrValues, oidcAcrValuesAppendix, oidcTriggerAcrValuesAppendixByUrlParameter, context.Request));

                                    return Task.CompletedTask;
                                }
                            };
                        }

                        // make sure we get user group membership information

                        openIdConnect.GetClaimsFromUserInfoEndpoint = true;

                        var scopes = new HashSet<string>();

                        scopes.Add("openid");
                        scopes.Add("email");
                        scopes.Add("phone");
                        scopes.Add("profile");

                        if (oidcScopes != null)
                        {
                            foreach (var oidcScope in oidcScopes)
                            {
                                scopes.Add(oidcScope);
                            }
                        }

                        foreach (var scope in scopes)
                        {
                            openIdConnect.Scope.Add(scope);
                        }
                    }
                });

                authenticationBuilder.AddSaml2(IdentityCoreConstants.ExternalSamlScheme, saml =>
                {
                    // will be configured dynamically
                });

                CryptoConfig.AddAlgorithm(typeof(RSAPKCS1SHA1SignatureDescription), SignedXml.XmlDsigRSASHA1Url);

                tenantsBuilder.WithPerTenantOptions<Saml2Options>((saml, tenantInfo, name) =>
                {
                    if (string.Equals(tenantInfo.ExternalAuthenticationMethod, TenantConstants.ExternalAuthenticationMethodSaml))
                    {
                        var samlEntityId = tenantInfo.SamlEntityId;

                        saml.SPOptions.EntityId = new EntityId(samlEntityId);
                        
                        saml.SPOptions.ReturnUrl = new Uri($"{tenantInfo.WebUrl}Account/Login?ReturnUrl=%2F&handler=ExternalCallback");

                        if (tenantInfo.SamlAllowWeakSigningAlgorithm)
                        {
                            // OK this is less secure, but sometimes we're having trouble
                            // e.g. with SSO Circle that still uses SHA1

                            saml.SPOptions.MinIncomingSigningAlgorithm = SignedXml.XmlDsigRSASHA1Url;
                        }

                        var samlCertificate = tenantInfo.GetSamlCertificate();

                        if (samlCertificate != null)
                        {
                            saml.SPOptions.ServiceCertificates.Add(new ServiceCertificate()
                            {
                                Certificate = samlCertificate,
                                Use = CertificateUse.Signing
                            });
                        }

                        var samlPeerEntityId = tenantInfo.SamlPeerEntityId;

                        var samlPeerIdpMetadataLocation = tenantInfo.SamlPeerIdpMetadataLocation;

                        if (!string.IsNullOrEmpty(samlPeerIdpMetadataLocation))
                        {
                            // the provider has configuration data available online

                            saml.IdentityProviders.Add(
                                new IdentityProvider(new EntityId(samlPeerEntityId), saml.SPOptions)
                                {
                                    LoadMetadata = true,
                                    MetadataLocation = samlPeerIdpMetadataLocation,
                                    AllowUnsolicitedAuthnResponse = true
                                }
                            );
                        }
                        else
                        {
                            // we need to get the configuration data from our DB
                            var samlPeerIdpMetadata = tenantInfo.SamlPeerIdpMetadata;

                            var metadata = Saml2MetadataLoader.LoadIdp(samlPeerIdpMetadata, saml.SPOptions.Compatibility.UnpackEntitiesDescriptorInIdentityProviderMetadata);

                            var identityProvider = new IdentityProvider(new EntityId(samlPeerEntityId), saml.SPOptions)
                            {
                                AllowUnsolicitedAuthnResponse = true
                            };

                            identityProvider.ReadMetadata(metadata);

                            saml.IdentityProviders.Add(identityProvider);
                        }

                        saml.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                        saml.SignOutScheme = IdentityServerConstants.SignoutScheme;

                        saml.Notifications.AcsCommandResultCreated = (commandResult, response) =>
                        {
                            if (response.Status == Saml2StatusCode.Success)
                            {
                                var identity = commandResult.Principal.Identity as ClaimsIdentity;

                                if (identity != null)
                                {
                                    Claim issuerClaim;

                                    while ((issuerClaim = identity.Claims.FirstOrDefault(claim => string.Equals(claim.Type, "issuer"))) != null) {
                                        identity.RemoveClaim(issuerClaim);
                                    }

                                    identity.AddClaim(new Claim("issuer", response.Issuer.Id));
                                }
                            }
                        };
                    }
                });
            }
        }

        private static string AdjustAcrValues(string oidcAcrValues, string oidcAcrValuesAppendix, bool oidcTriggerAcrValuesAppendixByUrlParameter, HttpRequest request)
        {
            if (string.IsNullOrEmpty(oidcAcrValuesAppendix))
                return oidcAcrValues;

            if (oidcTriggerAcrValuesAppendixByUrlParameter)
            {
                if (!request.Query.ContainsKey(TriggerAcrValuesGetParameterKey))
                    return oidcAcrValues;
            }

            return $"{oidcAcrValues}{oidcAcrValuesAppendix}";
        }

        private static bool CheckScopes(ClaimsPrincipal user, string[] requiredScopes)
        {
            foreach (var requiredScope in requiredScopes)
            {
                if (user.FindFirst(claim => claim.Type == JwtClaimTypes.Scope && claim.Value == requiredScope) == null)
                    return false;
            }

            return true;
        }

        private static void ConfigureAspNetIdentity(IServiceCollection services, TenantsBuilder tenantsBuilder, IConfiguration configuration)
        {
            // now, on second priority, comes the identity which we tweaked

            bool requireEmailConfirmed = configuration.GetValue<bool>("Identity:FeatureSet:RequireEmailConfirmed");

            var identityBuilder = services.AddIdentity<UserModel, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedEmail = requireEmailConfirmed;
            });

            // IdentityModelEventSource.ShowPII = true;

            identityBuilder.AddEntityFrameworkStores<SqlServerIdentityDbContext>();
            identityBuilder.AddDefaultTokenProviders();

            if (tenantsBuilder == null)
            {
                string authCookieDomain = configuration[$"Identity:AuthCookie:Domain"];
                if (string.IsNullOrEmpty(authCookieDomain))
                    throw new Exception("Identity auth cookie domain is empty");

                services.ConfigureApplicationCookie(options =>
                {
                    options.Cookie.Domain = authCookieDomain;
                    options.Cookie.Name = "HCore.Identity.session";
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    // was LAX
                    options.Cookie.SameSite = SameSiteMode.None;
                });
            } else
            {
                tenantsBuilder.WithPerTenantOptions<CookieAuthenticationOptions>((options, tenantInfo, name) =>
                {
                    options.Cookie.Domain = tenantInfo.DeveloperAuthCookieDomain;

                    if (!tenantInfo.UsersAreExternallyManaged)
                    {
                        options.Cookie.Name = $"{tenantInfo.DeveloperUuid}.HCore.Identity.session";
                    }
                    else
                    {
                        options.Cookie.Name = $"{tenantInfo.DeveloperUuid}.{tenantInfo.TenantUuid}.HCore.Identity.Session";
                    }

                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    // was LAX
                    options.Cookie.SameSite = SameSiteMode.None;
                });
            }

            services.Configure<IdentityOptions>(options =>
            {
                options.User.RequireUniqueEmail = true;

                // add ':' for our external user names
                options.User.AllowedUserNameCharacters = IdentityServicesImpl.AllowedUserNameCharacters;

                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 8;
            });
        }

        private static void ConfigureIdentityServer(IServiceCollection services, TenantsBuilder tenantsBuilder, string migrationsAssembly, IConfiguration configuration)
        {
            IIdentityServerBuilder identityServerBuilder;

            if (tenantsBuilder == null)
            {
                identityServerBuilder = services.AddIdentityServer(options =>
                {
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;
                    options.UserInteraction.ErrorUrl = "/Error";
                    options.UserInteraction.ConsentUrl = "/Account/Consent";
                });
            }
            else
            {
                string defaultClientAuthority = configuration[$"Identity:DefaultClient:Authority"];
                if (string.IsNullOrEmpty(defaultClientAuthority))
                    throw new Exception("Identity default client authority string is empty");

                identityServerBuilder = services.AddIdentityServer(options =>
                {
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;
                    options.UserInteraction.ErrorUrl = "/Error";
                    options.UserInteraction.ConsentUrl = "/Account/Consent";

                    options.IssuerUri = defaultClientAuthority;
                });
            }

            // see http://amilspage.com/signing-certificates-idsv4/

            identityServerBuilder.AddSigningCredential(GetSigningKeyCertificate(configuration));

            identityServerBuilder.AddAspNetIdentity<UserModel>();

            // this adds the config data from DB (clients, resources)
            identityServerBuilder.AddConfigurationStore<SqlServerConfigurationDbContext>(options =>
            {
                options.ConfigureDbContext = dbContextBuilder =>
                    dbContextBuilder.AddSqlDatabase("Identity", configuration, migrationsAssembly);
            });

            services.AddDbContext<ConfigurationDbContext>(options =>
            {
                options.AddSqlDatabase("Identity", configuration, migrationsAssembly);
            });

            // this adds the operational data from DB (codes, tokens, consents)
            identityServerBuilder.AddOperationalStore<SqlServerPersistedGrantDbContext>(options =>
            {
                options.ConfigureDbContext = dbContextBuilder =>
                    dbContextBuilder.AddSqlDatabase("Identity", configuration, migrationsAssembly);

                // this enables automatic token cleanup. this is optional.
                options.EnableTokenCleanup = true;
                // options.TokenCleanupInterval = 15; // frequency in seconds to cleanup stale grants. 15 is useful during debugging
            });

            services.AddDbContext<PersistedGrantDbContext>(options =>
            {
                options.AddSqlDatabase("Identity", configuration, migrationsAssembly);
            });

            identityServerBuilder.AddRedirectUriValidator<WildcardRedirectUriValidatorImpl>();
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
