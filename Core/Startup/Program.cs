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
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Linq;

namespace ReinhardHolzner.Core.Startup
{
    // Inspired from https://github.com/aspnet/MetaPackages/blob/2.1.3/src/Microsoft.AspNetCore/WebHost.cs

    public class Program
    {
        protected static void Launch<TStartup>(string[] args)
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

            var hostingConfig = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .Build();

            bool useHttps = hostingConfig.GetValue<bool>("UseHttps");
            int port = hostingConfig.GetValue<int>("Port");

            if (useWebListener)
            {
                // see https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/httpsys

                // do not forget to install the SSL certificate before, see SETUP.txt
                builder.UseHttpSys();
            } else {
                builder.UseKestrel((builderContext, options) =>
                {
                    options.Configure(builderContext.Configuration.GetSection("Kestrel"));

                    if (useHttps)
                    {
                        string httpsCertificateAssembly = null;
                        string httpsCertificateName = null;
                        string httpsCertificatePassword = null;

                        if (useHttps)
                        {
                            httpsCertificateAssembly = hostingConfig["HttpsCertificateAssembly"];
                            if (string.IsNullOrEmpty(httpsCertificateAssembly))
                                throw new Exception("HTTPS certificate assembly not found");

                            httpsCertificateName = hostingConfig["HttpsCertificateName"];

                            if (string.IsNullOrEmpty(httpsCertificateName))
                                throw new Exception("HTTPS certificate name not found");

                            httpsCertificatePassword = hostingConfig["HttpsCertificatePassword"];

                            if (string.IsNullOrEmpty(httpsCertificatePassword))
                                throw new Exception("HTTPS certificate password not found");
                        }

                        // from https://stackoverflow.com/questions/50708394/read-embedded-file-from-resource-in-asp-net-core

                        X509Certificate2 certificate = null;

                        Assembly httpsAssembly = AppDomain.CurrentDomain.GetAssemblies().
                            SingleOrDefault(assembly => assembly.GetName().Name == httpsCertificateAssembly);

                        if (httpsAssembly == null)
                            throw new Exception("HTTPS certificate assembly is not present in the list of assemblies");

                        var resourceStream = httpsAssembly.GetManifestResourceStream(httpsCertificateName);

                        if (resourceStream == null)
                            throw new Exception("HTTPS certificate resource not found");

                        using (var memory = new MemoryStream((int)resourceStream.Length))
                        {
                            resourceStream.CopyTo(memory);

                            certificate = new X509Certificate2(memory.ToArray(), httpsCertificatePassword);
                        }

                        options.Listen(IPAddress.Any, port, listenOptions =>
                            listenOptions.UseHttps(certificate));
                    }
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

            string serverUrl = useHttps ? "https://" : "http://";

            string domain = hostingConfig["Domain"];
            if (string.IsNullOrEmpty(domain))
                throw new Exception("Domain not found in application settings");

            serverUrl += domain;
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