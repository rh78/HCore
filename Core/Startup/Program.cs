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
using ReinhardHolzner.HCore.ElasticSearch.Impl;
using ReinhardHolzner.HCore.ElasticSearch;

namespace ReinhardHolzner.Core.Startup
{
    // Inspired from https://github.com/aspnet/MetaPackages/blob/2.1.3/src/Microsoft.AspNetCore/WebHost.cs

    public class Program
    {
        protected static void Launch<TStartup>(string[] args, IElasticSearchMappingInterface mappingInterface)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (environment == EnvironmentName.Development)
                CreateWebHostBuilder(environment, false, false, typeof(TStartup), args, mappingInterface).Build().Run();
            else if (environment == EnvironmentName.Staging)
                CreateWebHostBuilder(environment, false, true, typeof(TStartup), args, mappingInterface).Build().Run();
            else if (environment == EnvironmentName.Production)
                CreateWebHostBuilder(environment, true, true, typeof(TStartup), args, mappingInterface).Build().Run();
            else
                throw new Exception($"Invalid environment name found: {environment}");
        }

        private static IWebHostBuilder CreateWebHostBuilder(string environment, bool isProduction, bool useWebListener, Type startupType, string[] args, IElasticSearchMappingInterface mappingInterface)
        {
            var builder = new WebHostBuilder();

            var hostingConfig = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .Build();

            ConfigureServiceProviders(builder, isProduction, hostingConfig, mappingInterface);
            ConfigureLogging(builder);
            ConfigureContentRoot(builder);
            ConfigureConfiguration(builder, args);

            string serverUrl = ConfigureWebServer(builder, hostingConfig, useWebListener, startupType);

            Console.WriteLine($"Launching using server URL {serverUrl}");

            return builder;
        }

        private static void ConfigureServiceProviders(WebHostBuilder builder, bool isProduction, IConfigurationRoot hostingConfig, IElasticSearchMappingInterface mappingInterface)
        {
            ConfigureDefaultServiceProvider(builder);

            ConfigureElasticSearch(builder, isProduction, hostingConfig, mappingInterface);            
        }

        private static void ConfigureDefaultServiceProvider(WebHostBuilder builder)
        {
            builder.UseDefaultServiceProvider((context, options) =>
            {
                options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
            });
        }

        private static void ConfigureElasticSearch(WebHostBuilder builder, bool isProduction, IConfigurationRoot hostingConfig, IElasticSearchMappingInterface mappingInterface)
        {
            bool useElasticSearch = hostingConfig.GetValue<bool>("UseElasticSearch");

            if (useElasticSearch)
            {
                int numberOfShards = hostingConfig.GetValue<int>("ElasticSearch:Shards");
                if (numberOfShards < 1)
                    throw new Exception("ElasticSearch number of shards is invalid");

                int numberOfReplicas = hostingConfig.GetValue<int>("ElasticSearch:Replicas");
                if (numberOfReplicas < 1)
                    throw new Exception("ElasticSearch number of replicas is invalid");

                string hosts = hostingConfig["ElasticSearch:Hosts"];
                if (string.IsNullOrEmpty(hosts))
                    throw new Exception("ElasticSearch hosts not found");

                IElasticSearchClient elasticSearchClient = new ElasticSearchClient(
                    isProduction, numberOfShards, numberOfReplicas, hosts, mappingInterface);

                Console.WriteLine("Initializing ElasticSearch client...");

                elasticSearchClient.Initialize();

                Console.WriteLine("Initialized ElasticSearch client");

                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(elasticSearchClient);
                });               
            }            
        }

        private static void ConfigureLogging(WebHostBuilder builder)
        {
            builder.ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
                logging.AddDebug();
            });
        }

        private static void ConfigureContentRoot(WebHostBuilder builder)
        {
            if (string.IsNullOrEmpty(builder.GetSetting(WebHostDefaults.ContentRootKey)))
            {
                builder.UseContentRoot(Directory.GetCurrentDirectory());
            }
        }

        private static void ConfigureConfiguration(WebHostBuilder builder, string[] args)
        {
            if (args != null)
            {
                builder.UseConfiguration(new ConfigurationBuilder().AddCommandLine(args).Build());
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
            });
        }

        private static string ConfigureWebServer(WebHostBuilder builder, IConfigurationRoot hostingConfig, bool useWebListener, Type startupType)
        {
            bool useHttps = hostingConfig.GetValue<bool>("WebServer:UseHttps");
            int port = hostingConfig.GetValue<int>("WebServer:Port");

            if (useWebListener)
            {
                // see https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/httpsys

                // do not forget to install the SSL certificate before, see SETUP.txt
                builder.UseHttpSys();
            }
            else
            {
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
                            httpsCertificateAssembly = hostingConfig["WebServer:Https:Certificate:Assembly"];
                            if (string.IsNullOrEmpty(httpsCertificateAssembly))
                                throw new Exception("HTTPS certificate assembly not found");

                            httpsCertificateName = hostingConfig["WebServer:Https:Certificate:Name"];

                            if (string.IsNullOrEmpty(httpsCertificateName))
                                throw new Exception("HTTPS certificate name not found");

                            httpsCertificatePassword = hostingConfig["WebServer:Https:Certificate:Password"];

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

            builder.ConfigureServices((hostingContext, services) =>
            {
                var configuration = hostingContext.Configuration;

                // Fallback
                services.PostConfigure<HostFilteringOptions>(options =>
                {
                    if (options.AllowedHosts == null || options.AllowedHosts.Count == 0)
                    {
                        // "AllowedHosts": "localhost;127.0.0.1;[::1]"
                        var hosts = configuration["WebServer:AllowedHosts"]?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        // Fall back to "*" to disable.
                        options.AllowedHosts = (hosts?.Length > 0 ? hosts : new[] { "*" });
                    }
                });
                // Change notification
                services.AddSingleton<IOptionsChangeTokenSource<HostFilteringOptions>>(
                    new ConfigurationChangeTokenSource<HostFilteringOptions>(configuration));

                services.AddTransient<IStartupFilter, HostFilteringStartupFilter>();
            });

            builder.UseIISIntegration();

            string serverUrl = useHttps ? "https://" : "http://";

            string domain = hostingConfig["WebServer:Domain"];
            if (string.IsNullOrEmpty(domain))
                throw new Exception("Domain not found in application settings");

            serverUrl += domain;
            serverUrl += ":" + port;

            builder.UseUrls(new string[] { serverUrl });

            builder.UseApplicationInsights();

            builder.UseStartup(startupType);

            return serverUrl;
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