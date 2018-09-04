using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReinhardHolzner.Core.Middleware;
using Microsoft.AspNetCore.Http;
using System;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace ReinhardHolzner.Core.Startup
{
    public abstract class Startup
    {
        private bool _useHttps;
        private int _port;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        protected abstract void ConfigureCoreServices(IServiceCollection services);

        public void ConfigureServices(IServiceCollection services)
        {            
            ConfigureLocalization(services);
            ConfigureDataProtection(services);
            ConfigureWebServer(services);
            ConfigureCors(services);
            ConfigureJwt(services);
            ConfigureMvc(services);

            ConfigureCoreServices(services);
        }

        private void ConfigureLocalization(IServiceCollection services)
        {
            services.AddLocalization(options =>
            {
                options.ResourcesPath = "Resources";
            });
        }

        private void ConfigureDataProtection(IServiceCollection services)
        {
            /* Not necessary for now as we do not generate cookies in the backend
            
            // see https://docs.microsoft.com/en-us/aspnet/core/security/data-protection

            string assemblyName = Assembly.GetEntryAssembly().FullName;
            
            services.AddDataProtection()
                .SetApplicationName(assemblyName)
                .PersistKeysToAzureBlobStorage(new Uri(""))               
                .ProtectKeysWithAzureKeyVault("", "", ""); */

        }

        private void ConfigureWebServer(IServiceCollection services)
        {
            _useHttps = Configuration.GetValue<bool>("UseHttps");
            _port = Configuration.GetValue<int>("Port");

            if (_useHttps)
            {
                services.AddHsts(options =>
                {
                    options.Preload = true;
                    options.IncludeSubDomains = true;
                    options.MaxAge = TimeSpan.FromDays(60);
                });

                services.AddHttpsRedirection(options =>
                {
                    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
                    options.HttpsPort = _port;
                });              
            }

            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
            });
        }

        private void ConfigureCors(IServiceCollection services)
        {
            /* We do not need to introduce CORS headers, because they're done by Apigee for now...
             
            // see: https://docs.microsoft.com/en-us/aspnet/core/security/cors

            services.AddCors(); */
        }

        private void ConfigureJwt(IServiceCollection services)
        {
            bool useJwt = Configuration.GetValue<bool>("WebServer:UseJwt");

            if (useJwt)
            {
                JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

                string jwtAuthority = Configuration["WebServer:Jwt:Authority"];
                if (string.IsNullOrEmpty(jwtAuthority))
                    throw new Exception("JWT authority not found");

                string jwtAudience = Configuration["WebServer:Jwt:Audience"];
                if (string.IsNullOrEmpty(jwtAudience))
                    throw new Exception("JWT audience not found");

                services
                    .AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                    .AddJwtBearer(options =>
                    {
                        // see https://developer.okta.com/blog/2018/03/23/token-authentication-aspnetcore-complete-guide

                        options.Authority = jwtAuthority;
                        options.Audience = jwtAudience;                        
                    });
            }           
        }

        private void ConfigureMvc(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            ConfigureLogging(app, env);
            ConfigureHttps(app, env);
            ConfigureResponseCompression(app, env);
            ConfigureStaticFiles(app, env);
            ConfigureCors(app, env);
            ConfigureCsp(app, env);
            ConfigureRequestLocalization(app, env);
            ConfigureExceptionHandling(app, env);
            ConfigureMvc(app, env);
        }

        private void ConfigureLogging(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();            
        }

        public void ConfigureHttps(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (_useHttps)
            {
                if (!env.IsDevelopment())
                    app.UseHsts();

                app.UseHttpsRedirection();
            }
        }

        private void ConfigureResponseCompression(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseResponseCompression();
        }

        private void ConfigureStaticFiles(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = context =>
                {
                    var cacheControlHeaderValue = new CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = TimeSpan.FromDays(365)
                    };

                    context.Context.Response.GetTypedHeaders().CacheControl = cacheControlHeaderValue;
                }
            });
        }

        private void ConfigureCors(IApplicationBuilder app, IHostingEnvironment env)
        {
            /* We do not need to introduce CORS headers, because they're done by Apigee for now...
             
            // see: https://docs.microsoft.com/en-us/aspnet/core/security/cors

            app.UseCors(builder => 
            {
                // TODO (how to handle '*' with credentials?)
            }); */
        }

        private void ConfigureCsp(IApplicationBuilder app, IHostingEnvironment env)
        {
            /* We do not need to introduce Content Security Policy headers, because we're going
               through Apigee for now

            app.UseMiddleware<CspHandlingMiddleware>(); */
        }

        private void ConfigureRequestLocalization(IApplicationBuilder app, IHostingEnvironment env)
        {
            var englishCultureInfo = new CultureInfo("en");
            var germanCultureInfo = new CultureInfo("de");

            var cultures = new CultureInfo[] { englishCultureInfo, germanCultureInfo };

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(englishCultureInfo),
                SupportedCultures = cultures,
                SupportedUICultures = cultures
            });
        }

        private void ConfigureExceptionHandling(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMiddleware<UnhandledExceptionHandlingMiddleware>();
        }

        private void ConfigureMvc(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMvc();
        }
    }
}
