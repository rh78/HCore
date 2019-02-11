using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HCore.Web.Middleware;
using Microsoft.AspNetCore.Http;
using System;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.AspNetCore.Routing;
using HCore.Web.Providers.Impl;
using HCore.Web.Providers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using HCore.Web.Json;
using Microsoft.AspNetCore.StaticFiles;

namespace HCore.Web.Startup
{
    public abstract class Startup
    {
        private bool _useHttps;
        private int _port;
        
        public Startup(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
        }

        public IConfiguration Configuration { get; }
        public IHostingEnvironment HostingEnvironment { get; set; }

        protected virtual void ConfigureCoreServices(IServiceCollection services)
        {

        }

        protected virtual void ConfigureCore(IApplicationBuilder app)
        {

        }

        protected virtual void ConfigureCoreRoutes(IRouteBuilder routes)
        {

        }

        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureLogging(services);
            ConfigureHttpContextAccessor(services);
            ConfigureLocalization(services);
            ConfigureUrlHelper(services);
            ConfigureWebServer(services);
            ConfigureStaticFiles(services);
            ConfigureMvc(services);

            ConfigureGenericServices(services);

            ConfigureCoreServices(services);            
        }

        private void ConfigureLogging(IServiceCollection services)
        {
            bool useSegment = Configuration.GetValue<bool>("WebServer:UseSegment");

            if (useSegment)
            {
                services.AddSegment(Configuration);
            }
        }

        protected virtual void ConfigureHttpContextAccessor(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
        }

        protected virtual void ConfigureLocalization(IServiceCollection services)
        {
            var englishCultureInfo = CultureInfo.GetCultureInfo("en");
            var germanCultureInfo = CultureInfo.GetCultureInfo("de");

            var cultures = new CultureInfo[] { englishCultureInfo, germanCultureInfo };

            services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new RequestCulture(englishCultureInfo);
                options.SupportedCultures = cultures;
                options.SupportedUICultures = cultures;
            });

            services.AddLocalization(options =>
            {
                options.ResourcesPath = "Resources";
            });

            services.AddCoreTranslations();
        }

        protected virtual void ConfigureUrlHelper(IServiceCollection services)
        {
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

            services.AddScoped(x => {
                var actionContext = x.GetRequiredService<IActionContextAccessor>().ActionContext;
                var factory = x.GetRequiredService<IUrlHelperFactory>();
                return factory.GetUrlHelper(actionContext);
            });
        }

        protected virtual void ConfigureWebServer(IServiceCollection services)
        {
            bool useWeb = Configuration.GetValue<bool>("UseWeb");

            if (useWeb)
            {
                // configure cookie policies

                services.Configure<CookiePolicyOptions>(options =>
                {
                    // This lambda determines whether user consent for non-essential cookies is needed for a given request.

                    options.CheckConsentNeeded = context => true;
                    options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.None;
                });
            }

            _useHttps = Configuration.GetValue<bool>("WebServer:UseHttps");
            _port = Configuration.GetValue<int>("WebServer:WebPort");

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

        protected virtual void ConfigureStaticFiles(IServiceCollection services)
        {
            bool useSpa = Configuration.GetValue<bool>("WebServer:UseSpa");

            if (useSpa)
            {
                services.AddSingleton<ISpaManifestJsonProvider, SpaManifestJsonProviderImpl>();

                bool staticFiles = Configuration.GetValue<bool>("Spa:StaticFiles");

                if (staticFiles)
                {
                    services.AddSpaStaticFiles(configuration =>
                    {
                        configuration.RootPath = "ClientApp/build";
                    });
                }
            }            
        }

        protected virtual void ConfigureMvc(IServiceCollection services)
        {
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Include;
                    options.SerializerSettings.ContractResolver = IgnoreOutboundNullValuesContractResolver.Instance;
                });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            ConfigureExceptionHandling(app, env);

            ConfigureCore(app);

            ConfigureLogging(app, env);
            ConfigureLocalization(app, env);
            ConfigureHttps(app, env);
            ConfigureResponseCompression(app, env);
            ConfigureStaticFiles(app, env);
            ConfigureCsp(app, env);
            ConfigureRequestLocalization(app, env);                

            ConfigureMvc(app, env);
        }

        protected virtual void ConfigureLogging(IApplicationBuilder app, IHostingEnvironment env)
        {
            bool useSegment = Configuration.GetValue<bool>("WebServer:UseSegment");

            if (useSegment)
            {
                app.UseSegment();
            }
        }

        protected virtual void ConfigureLocalization(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseCoreTranslations();
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

        protected virtual void ConfigureResponseCompression(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseResponseCompression();
        }

        protected virtual void ConfigureStaticFiles(IApplicationBuilder app, IHostingEnvironment env)
        {
            var provider = new FileExtensionContentTypeProvider();

            provider.Mappings[".yaml"] = "application/x-yaml";

            var staticFileOptions = new StaticFileOptions
            {
                OnPrepareResponse = context =>
                {
                    var cacheControlHeaderValue = new CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = TimeSpan.FromDays(365)
                    };

                    context.Context.Response.GetTypedHeaders().CacheControl = cacheControlHeaderValue;
                },
                ContentTypeProvider = provider
            };
            
            app.UseStaticFiles(staticFileOptions);

            bool useSpa = Configuration.GetValue<bool>("WebServer:UseSpa");

            if (useSpa)
            {
                bool staticFiles = Configuration.GetValue<bool>("Spa:StaticFiles");

                if (staticFiles)
                {
                    app.UseSpaStaticFiles(staticFileOptions);
                }
            }
        }

        protected virtual void ConfigureCsp(IApplicationBuilder app, IHostingEnvironment env)
        {
            bool useCsp = Configuration.GetValue<bool>("WebServer:UseCsp");

            if (useCsp)
            {
                app.UseMiddleware<CspHandlingMiddleware>();
            }
        }

        protected virtual void ConfigureRequestLocalization(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseRequestLocalization();
        }

        protected virtual void ConfigureExceptionHandling(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMiddleware<UnhandledExceptionHandlingMiddleware>();
        }

        protected virtual void ConfigureMvc(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMvc(routes =>
            {
                ConfigureCoreRoutes(routes);
            });            
        }

        protected virtual void ConfigureGenericServices(IServiceCollection services)
        {
            services.AddScoped<IUrlProvider, UrlProviderImpl>();
            services.AddScoped<INonHttpContextUrlProvider, NonHttpContextUrlProviderImpl>();
            services.AddScoped<INowProvider, NowProviderImpl>();
        }
    }
}
