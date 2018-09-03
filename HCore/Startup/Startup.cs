using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmintIo.CLAPI.Consumer.Generated.Controllers;
using SmintIo.CLAPI.Consumer.Controllers.Impl;
using ReinhardHolzner.HCore.Middleware;
using Microsoft.AspNetCore.Http;
using System;

namespace SmintIo.CLAPI.Consumer
{
    public class Startup
    {
        private bool _useHttps;
        private int _port;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddSingleton<IConnectionsApiController, ConnectionsApiImpl>();

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
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            if (_useHttps)
            {
                app.UseHttpsRedirection();
            }

            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMiddleware<UnhandledExceptionHandlingMiddleware>();

            app.UseMvc();
        }
    }
}
