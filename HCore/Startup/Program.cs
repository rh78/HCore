using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ReinhardHolzner.HCore.Startup
{
    // Inspired from https://github.com/aspnet/MetaPackages/blob/2.1.3/src/Microsoft.AspNetCore/WebHost.cs

    public class Program
    {
        protected static void LaunchHCore<TStartup>(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (environment == EnvironmentName.Development)
                CreateWebHostBuilder(environment, false, typeof(TStartup), args).Build().Run();
            else if (environment == EnvironmentName.Staging)
                CreateWebHostBuilder(environment, true, typeof(TStartup), args).Build().Run();
            else if (environment == EnvironmentName.Production)
                CreateWebHostBuilder(environment, true, typeof(TStartup), args).Build().Run();
            else
                throw new Exception("Invalid environment name found: " + environment);
        }

        private static IWebHostBuilder CreateWebHostBuilder(string environment, bool useWebListener, Type startupType, string[] args)
        {
            var builder = new WebHostBuilder();

            if (string.IsNullOrEmpty(builder.GetSetting(WebHostDefaults.ContentRootKey)))
            {
                builder.UseContentRoot(Directory.GetCurrentDirectory());
            }
            if (args != null)
            {
                builder.UseConfiguration(new ConfigurationBuilder().AddCommandLine(args).Build());
            }

            if (useWebListener)
            {
                // see https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/httpsys

                builder.UseHttpSys();
            } else {
                builder.UseKestrel((builderContext, options) =>
                {
                    options.Configure(builderContext.Configuration.GetSection("Kestrel"));
                });
            }
            
            builder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                var env = hostingContext.HostingEnvironment;

                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

                if (env.IsDevelopment())
                {
                    var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                    if (appAssembly != null)
                    {
                        config.AddUserSecrets(appAssembly, optional: true);
                    }
                }

                config.AddEnvironmentVariables();

                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            })
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
                logging.AddDebug();
            })
            .ConfigureServices((hostingContext, services) =>
            {
                var configuration = hostingContext.Configuration;

                // Fallback
                services.PostConfigure<HostFilteringOptions>(options =>
                {
                    if (options.AllowedHosts == null || options.AllowedHosts.Count == 0)
                    {
                        // "AllowedHosts": "localhost;127.0.0.1;[::1]"
                        var hosts = configuration["AllowedHosts"]?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        // Fall back to "*" to disable.
                        options.AllowedHosts = (hosts?.Length > 0 ? hosts : new[] { "*" });
                    }
                });
                // Change notification
                services.AddSingleton<IOptionsChangeTokenSource<HostFilteringOptions>>(
                    new ConfigurationChangeTokenSource<HostFilteringOptions>(configuration));

                services.AddTransient<IStartupFilter, HostFilteringStartupFilter>();                
            })
            .UseIISIntegration()
            .UseDefaultServiceProvider((context, options) =>
            {
                options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
            });

            var hostingConfig = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .Build();

            bool useHttps = hostingConfig.GetValue<bool>("UseHttps");

            string serverUrl = useHttps ? "https://" : "http://";

            string domain = hostingConfig["Domain"];
            if (string.IsNullOrEmpty(domain))
                throw new Exception("Domain not found in application settings");

            serverUrl += domain;

            int port = hostingConfig.GetValue<int>("Port");

            serverUrl += ":" + port;

            builder.UseUrls(new string[] { serverUrl });

            builder.UseStartup(startupType);

            Console.WriteLine("Launching using server URL: " + serverUrl);

            return builder;
        }
    }

    internal class HostFilteringStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.UseHostFiltering();
                next(app);
            };
        }
    }
}