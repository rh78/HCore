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
using System.Collections.Generic;
using System.Threading;

namespace HCore.Web.Startup
{
    // Inspired from https://github.com/aspnet/MetaPackages/blob/2.1.3/src/Microsoft.AspNetCore/WebHost.cs

    public class Launcher<TStartup>
    {
        private string _environment;        
        
        private WebHostBuilder _builder;
        private IConfigurationRoot _configuration;

        private string _serverUrl;

        public string[] Args { get; set; }

        public Launcher(string[] args)
        {
            Args = args;
        }

        public void Launch()
        {
            _environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            
            CreateWebHostBuilder();
            
            Console.WriteLine($"Launching using server URL {_serverUrl}");

            _builder.Build().Run();
        }

        private void CreateWebHostBuilder()
        {
            _builder = new WebHostBuilder();

            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{_environment}.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{_environment}.local.json", optional: true, reloadOnChange: true)
                .Build();

            ConfigureDefaultServiceProvider();
            ConfigureLogging();
            ConfigureContentRoot();
            ConfigureConfiguration();
        
            ConfigureWebServer();                        
        }

        private void ConfigureDefaultServiceProvider()
        {
            _builder.UseDefaultServiceProvider((context, options) =>
            {
                options.ValidateScopes = true;
            });
        }        

        private void ConfigureLogging()
        {
            _builder.ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
                logging.AddDebug();
            });
        }

        private void ConfigureContentRoot()
        {
            if (string.IsNullOrEmpty(_builder.GetSetting(WebHostDefaults.ContentRootKey)))
            {
                _builder.UseContentRoot(Directory.GetCurrentDirectory());
            }
        }

        private void ConfigureConfiguration()
        {
            if (Args != null)
            {
                _builder.UseConfiguration(new ConfigurationBuilder().AddCommandLine(Args).Build());
            }

            _builder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                var env = hostingContext.HostingEnvironment;

                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{env.EnvironmentName}.local.json", optional: true, reloadOnChange: true);

                if (env.IsDevelopment())
                {
                    var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                    if (appAssembly != null)
                    {
                        config.AddUserSecrets(appAssembly, optional: true);
                    }
                }

                config.AddEnvironmentVariables();

                if (Args != null)
                {
                    config.AddCommandLine(Args);
                }
            });
        }

        private void ConfigureWebServer()
        {
            bool useHttps = _configuration.GetValue<bool>("WebServer:UseHttps");
            bool useWeb = _configuration.GetValue<bool>("WebServer:UseWeb");
            bool useApi = _configuration.GetValue<bool>("WebServer:UseApi");

            if (!useWeb && !useApi)
                throw new Exception("Please specify which kind of service (web or API) you want to use");

            int numberOfCores = _configuration.GetValue<int>("WebServer:NumberOfCores");

            if (numberOfCores <= 0)
                throw new Exception("Please specify the number of cores that the server provides");

            long maxRequestBodySizeKB = _configuration.GetValue<long>("WebServer:MaxRequestBodySizeKB");

            // see https://www.monitis.com/blog/improving-asp-net-performance-part3-threading/

            ThreadPool.SetMinThreads(88 * numberOfCores, 88 * numberOfCores);
            ThreadPool.SetMaxThreads(250 * numberOfCores, 250 * numberOfCores);

            int minWorkerThreads, minWorkerIoThreads;
            int maxWorkerThreads, maxWorkerIoThreads;

            ThreadPool.GetMinThreads(out minWorkerThreads, out minWorkerIoThreads);
            ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxWorkerIoThreads);

            Console.WriteLine($"Thread limits: {minWorkerThreads}/{minWorkerIoThreads} - {maxWorkerThreads}/{maxWorkerIoThreads}");

            ServicePointManager.UseNagleAlgorithm = false;

            // see https://support.microsoft.com/de-de/help/821268/contention-poor-performance-and-deadlocks-when-you-make-calls-to-web-s

            ServicePointManager.DefaultConnectionLimit = 12 * numberOfCores;

            Console.WriteLine($"Default connection limit {ServicePointManager.DefaultConnectionLimit}");

            int availableWorkerThreads, availableWorkerIoThreads;

            ThreadPool.GetAvailableThreads(out availableWorkerThreads, out availableWorkerIoThreads);

            Console.WriteLine($"Available threads: {availableWorkerThreads}/{availableWorkerThreads}");

            _builder.UseKestrel((builderContext, options) =>
            {
                options.Configure(builderContext.Configuration.GetSection("Kestrel"));

                // try to avoid thread starvation problems

                // see https://www.strathweb.com/2019/02/be-careful-when-manually-handling-json-requests-in-asp-net-core/

                // TODO enable in ASP.NET Core 3.0, where it should work - for 2.2 it has too many side effects

                // options.AllowSynchronousIO = false;

                // see https://www.monitis.com/blog/improving-asp-net-performance-part3-threading/
                
                options.Limits.MaxConcurrentConnections = 24 * numberOfCores;
                options.Limits.MaxConcurrentUpgradedConnections = 24 * numberOfCores;
                
                if (maxRequestBodySizeKB > 0)
                    options.Limits.MaxRequestBodySize = maxRequestBodySizeKB * 1024;
                
                if (useHttps)
                {
                    string httpsCertificateAssembly = null;
                    string httpsCertificateName = null;
                    string httpsCertificatePassword = null;

                    if (useHttps)
                    {
                        httpsCertificateAssembly = _configuration["WebServer:Https:Certificate:Assembly"];
                        if (string.IsNullOrEmpty(httpsCertificateAssembly))
                            throw new Exception("HTTPS certificate assembly not found");

                        httpsCertificateName = _configuration["WebServer:Https:Certificate:Name"];

                        if (string.IsNullOrEmpty(httpsCertificateName))
                            throw new Exception("HTTPS certificate name not found");

                        httpsCertificatePassword = _configuration["WebServer:Https:Certificate:Password"];

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

                    if (useWeb)
                    {
                        int webPort = _configuration.GetValue<int>("WebServer:WebPort");

                        options.Listen(IPAddress.Any, webPort, listenOptions =>
                            listenOptions.UseHttps(certificate));

                        int redirectHttpToHttpsWebPort = _configuration.GetValue<int>("WebServer:RedirectHttpToHttpsWebPort");

                        if (redirectHttpToHttpsWebPort > 0)
                            options.Listen(IPAddress.Any, redirectHttpToHttpsWebPort);
                    }

                    if (useApi)
                    { 
                        int apiPort = _configuration.GetValue<int>("WebServer:ApiPort");

                        options.Listen(IPAddress.Any, apiPort, listenOptions =>
                            listenOptions.UseHttps(certificate));
                    }                        
                }
            });            

            _builder.ConfigureServices((hostingContext, services) =>
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

            _builder.UseIISIntegration();

            List<string> urls = new List<string>();

            string urlPattern = _configuration["WebServer:UrlPattern"];
            if (string.IsNullOrEmpty(urlPattern))
                throw new Exception("URL pattern not found in application settings");

            if (useWeb)
            {
                string webServerUrl = useHttps ? "https://" : "http://";

                int webPort = _configuration.GetValue<int>("WebServer:WebPort");

                webServerUrl += urlPattern;
                webServerUrl += ":" + webPort;

                urls.Add(webServerUrl);

                if (useHttps)
                {
                    int redirectHttpToHttpsWebPort = _configuration.GetValue<int>("WebServer:RedirectHttpToHttpsWebPort");

                    if (redirectHttpToHttpsWebPort > 0)
                    {
                        webServerUrl = useHttps ? "https://" : "http://";

                        webServerUrl += urlPattern;
                        webServerUrl += ":" + redirectHttpToHttpsWebPort;

                        urls.Add(webServerUrl);
                    }
                }
            }

            if (useApi)
            { 
                string apiServerUrl = useHttps ? "https://" : "http://";                

                int apiPort = _configuration.GetValue<int>("WebServer:ApiPort");

                apiServerUrl += urlPattern;
                apiServerUrl += ":" + apiPort;

                urls.Add(apiServerUrl);
            }

            string[] urlsArray = urls.ToArray<string>();

            _builder.UseUrls(urlsArray);
            
            _builder.UseApplicationInsights();
            
            _builder.UseStartup(typeof(TStartup));

            _serverUrl = string.Join(" / ", urlsArray);            
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