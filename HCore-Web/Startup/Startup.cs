using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace HCore.Web.Startup
{
    public abstract class Startup
    {
        private bool _useHttps;
        private int _port;
        
        public Startup(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;

            UseSpa = Configuration.GetValue<bool>("WebServer:UseSpa");
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment HostingEnvironment { get; set; }

        public bool UseSpa { get; private set; }

        protected virtual void ConfigureCoreServices(IServiceCollection services)
        {

        }

        protected virtual void ConfigureCore(IApplicationBuilder app)
        {

        }

        protected virtual void ConfigureCoreIdentity(IApplicationBuilder app)
        {

        }

        protected virtual void ConfigureCoreRoutes(IEndpointRouteBuilder routes)
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
            ConfigureCookiePolicy(services);
            ConfigureMvc(services);

            ConfigureGenericServices(services);

            ConfigureCoreServices(services);            
        }

        private void ConfigureLogging(IServiceCollection services)
        {
#if !DEBUG
            services.AddApplicationInsightsTelemetry();
#endif

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

            CultureInfo[] supportedCultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures)
                .Where(cultureInfo => !string.IsNullOrEmpty(cultureInfo.Name))
                .ToArray();

            services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new RequestCulture(englishCultureInfo);

                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
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
                services.AddAntiforgery(options =>
                {
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    // was STRICT
                    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
                    options.SuppressXFrameOptionsHeader = true;
                });

                services.AddHsts(options =>
                {
                    options.Preload = true;
                    options.IncludeSubDomains = true;
                    options.MaxAge = TimeSpan.FromDays(180);
                });

                int httpHealthCheckPort = Configuration.GetValue<int>("WebServer:HttpHealthCheckPort");

                if (httpHealthCheckPort <= 0)
                {
                    // we can not do redirects to HTTPS if we have a health check running

                    int redirectHttpToHttpsTargetWebPort = Configuration.GetValue<int>("WebServer:RedirectHttpToHttpsTargetWebPort");

                    if (redirectHttpToHttpsTargetWebPort > 0)
                    {
                        services.AddHttpsRedirection(options =>
                        {
                            options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
                            options.HttpsPort = redirectHttpToHttpsTargetWebPort;
                        });
                    }
                    else
                    {
                        services.AddHttpsRedirection(options =>
                        {
                            options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
                            options.HttpsPort = _port;
                        });
                    }
                }
            }

            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
            });
        }

        protected virtual void ConfigureStaticFiles(IServiceCollection services)
        {
            if (UseSpa)
            {
                services.AddSingleton<IHtmlIncludesDetectorProvider, HtmlIncludesTemplateDetectorProviderImpl>();

                bool staticFiles = Configuration.GetValue<bool>("Spa:StaticFiles");

                if (staticFiles)
                {
                    services.AddSpaStaticFiles(configuration =>
                    {
                        configuration.RootPath = Configuration.GetValue<string>("Spa:RootPath");
                    });
                }
            }            
        }

        private void ConfigureCookiePolicy(IServiceCollection services)
        {
            services.ConfigureNonBreakingSameSiteCookies();
        }

        protected virtual void ConfigureMvc(IServiceCollection services)
        {
            services.AddControllersWithViews()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Include;
                    options.SerializerSettings.ContractResolver = IgnoreOutboundNullValuesContractResolver.Instance;
                });

            services.AddRazorPages()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Include;
                    options.SerializerSettings.ContractResolver = IgnoreOutboundNullValuesContractResolver.Instance;
                });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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

            if (UseSpa)
            {
                // Enforce creating the detector, as it will load all files initially, so errors will be visible right
                // at the startup.
                app.ApplicationServices.GetService<IHtmlIncludesDetectorProvider>();
            }
        }

        protected virtual void ConfigureLogging(IApplicationBuilder app, IWebHostEnvironment env)
        {
            bool useSegment = Configuration.GetValue<bool>("WebServer:UseSegment");

            if (useSegment)
            {
                app.UseSegment();
            }
        }

        protected virtual void ConfigureLocalization(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCoreTranslations();
        }

        public void ConfigureHttps(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (_useHttps)
            {
                if (!env.IsDevelopment())
                    app.UseHsts();

                int httpHealthCheckPort = Configuration.GetValue<int>("WebServer:HttpHealthCheckPort");

                if (httpHealthCheckPort <= 0)
                {
                    // we can not do redirects to HTTPS if we have a health check running

                    app.UseHttpsRedirection();
                }
            }
        }

        protected virtual void ConfigureResponseCompression(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseResponseCompression();
        }

        protected virtual void ConfigureStaticFiles(IApplicationBuilder app, IWebHostEnvironment env)
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

            if (UseSpa)
            {
                bool staticFiles = Configuration.GetValue<bool>("Spa:StaticFiles");

                if (staticFiles)
                {
                    app.UseSpaStaticFiles(staticFileOptions);
                }
            }
        }

        protected virtual void ConfigureCsp(IApplicationBuilder app, IWebHostEnvironment env)
        {
            bool useCsp = Configuration.GetValue<bool>("WebServer:UseCsp");

            if (useCsp)
            {
                app.UseMiddleware<CspHandlingMiddleware>();
            }
        }

        protected virtual void ConfigureRequestLocalization(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRequestLocalization();
        }

        protected virtual void ConfigureExceptionHandling(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<UnhandledExceptionHandlingMiddleware>();
        }

        protected virtual void ConfigureMvc(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseCookiePolicy();

            ConfigureCoreIdentity(app);

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();

                ConfigureCoreRoutes(endpoints);
            });            
        }

        protected virtual void ConfigureGenericServices(IServiceCollection services)
        {
            services.AddScoped<IUrlProvider, UrlProviderImpl>();
            services.AddScoped<INonHttpContextUrlProvider, NonHttpContextUrlProviderImpl>();
            services.AddScoped<INowProvider, NowProviderImpl>();
            services.AddSingleton<IDownloadProcessingProxyUrlProvider, DownloadProcessingProxyUrlProviderImpl>();

            if (UseSpa)
            {
                services.AddScoped((serviceProvider) =>
                {
                    var detector = serviceProvider.GetRequiredService<IHtmlIncludesDetectorProvider>();
                    var contextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                    return detector.HtmlIncludesProviderForRequest(contextAccessor?.HttpContext);
                });
            }
        }
    }
}
