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
using ReinhardHolzner.Core.RestSharp;
using ReinhardHolzner.Core.RestSharp.Impl;
using System.Collections.Generic;
using ReinhardHolzner.Core.Database.ElasticSearch;
using ReinhardHolzner.Core.Database.ElasticSearch.Impl;
using Microsoft.EntityFrameworkCore;
using ReinhardHolzner.Core.AMQP;
using ReinhardHolzner.Core.AMQP.Internal;
using ReinhardHolzner.Core.AMQP.Internal.Impl;

namespace ReinhardHolzner.Core.Startup
{
    // Inspired from https://github.com/aspnet/MetaPackages/blob/2.1.3/src/Microsoft.AspNetCore/WebHost.cs

    public class Launcher<TStartup, TSqlServerDbContext, TMessage> where TSqlServerDbContext : DbContext
    {
        private string _environment;
        private bool _isProduction;
        private bool _useWebListener;

        private WebHostBuilder _builder;
        private IConfigurationRoot _configuration;

        private string _serverUrl;

        public IElasticSearchDbContext ElasticSearchDbContext { get; set; }
        
        public string[] Args { get; set; }

        public Launcher(string[] args)
        {
            Args = args;
        }

        public void Launch()
        {
            _environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            _isProduction = false;
            _useWebListener = true;

            if (_environment == EnvironmentName.Development)
            {
                _useWebListener = false;

                CreateWebHostBuilder();
            }
            else if (_environment == EnvironmentName.Staging)
            {
                CreateWebHostBuilder();
            }
            else if (_environment == EnvironmentName.Production)
            {
                _isProduction = true;

                CreateWebHostBuilder();
            }
            else
            {
                throw new Exception($"Invalid environment name found: {_environment}");
            }

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
                .Build();


            ConfigureServiceProviders();
            ConfigureLogging();
            ConfigureContentRoot();
            ConfigureConfiguration();

            ConfigureWebServer();                        
        }
    
        private void ConfigureServiceProviders()
        {
            ConfigureDefaultServiceProvider();

            ConfigureSqlServer();
            ConfigureElasticSearch();
            ConfigureAmqp();            
        }

        private void ConfigureDefaultServiceProvider()
        {
            _builder.UseDefaultServiceProvider((context, options) =>
            {
                options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
            });
        }

        private void ConfigureSqlServer()
        {
            bool useSqlServer = _configuration.GetValue<bool>("UseSqlServer");

            if (useSqlServer)
            {
                _builder.ConfigureServices(services =>
                {
                    Console.WriteLine("Initializing SQL Server DB context...");

                    string connectionString = _configuration["SqlServer:ConnectionString"];
                    if (string.IsNullOrEmpty(connectionString))
                        throw new Exception("SQL Server connection string is empty");

                    services.AddDbContext<TSqlServerDbContext>(options =>
                    {
                        options.UseSqlServer(connectionString);
                    });

                    Console.WriteLine("Initialized SQL Server DB context");
                });
            }
        }

        private void ConfigureElasticSearch()
        {
            bool useElasticSearch = _configuration.GetValue<bool>("UseElasticSearch");

            if (useElasticSearch)
            {
                _builder.ConfigureServices(services =>
                {
                    Console.WriteLine("Initializing ElasticSearch client...");

                    if (ElasticSearchDbContext == null)
                        throw new Exception("ElasticSearch DB context is not set up");

                    int numberOfShards = _configuration.GetValue<int>("ElasticSearch:Shards");
                    if (numberOfShards < 1)
                        throw new Exception("ElasticSearch number of shards is invalid");

                    int numberOfReplicas = _configuration.GetValue<int>("ElasticSearch:Replicas");
                    if (numberOfReplicas < 1)
                        throw new Exception("ElasticSearch number of replicas is invalid");

                    string hosts = _configuration["ElasticSearch:Hosts"];
                    if (string.IsNullOrEmpty(hosts))
                        throw new Exception("ElasticSearch hosts not found");

                    IElasticSearchClient elasticSearchClient = new ElasticSearchClientImpl(
                        _isProduction, numberOfShards, numberOfReplicas, hosts, ElasticSearchDbContext);                    

                    elasticSearchClient.Initialize();                    

                    services.AddSingleton(elasticSearchClient);

                    Console.WriteLine("Initialized ElasticSearch client");
                });               
            }            
        }

        private void ConfigureAmqp()
        {
            bool useAmqpListener = _configuration.GetValue<bool>("UseAmqpListener");
            bool useAmqpSender = _configuration.GetValue<bool>("UseAmqpSender");

            if (useAmqpListener || useAmqpSender)
            {
                _builder.ConfigureServices(services =>
                {
                    Console.WriteLine("Initializing AMQP...");

                    string implementation = _configuration["Amqp:Implementation"];

                    if (string.IsNullOrEmpty(implementation))
                        throw new Exception("AMQP implementation specification is empty");

                    bool useServiceBus = string.Equals(implementation, "ServiceBus");

                    string connectionString = _configuration["Amqp:ConnectionString"];

                    if (string.IsNullOrEmpty(connectionString))
                        throw new Exception("AMQP connection string is empty");

                    string addresses = _configuration["Amqp:Addresses"];

                    if (string.IsNullOrEmpty(addresses))
                        throw new Exception("AMQP addresses are missing");

                    string[] addressesSplit = addresses.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                    if (addressesSplit.Length == 0)
                        throw new Exception("AMQP addresses are empty");                    

                    services.AddSingleton(factory =>
                    {
                        IAMQPMessageProcessor<TMessage> messageProcessor = factory.GetService<IAMQPMessageProcessor<TMessage>>();

                        if (useAmqpListener && messageProcessor == null)
                            throw new Exception("AMQP message processor service is not available");

                        IAMQPMessenger<TMessage> amqpMessenger;

                        if (useServiceBus)
                        {
                            // Service Bus
                            amqpMessenger = new ServiceBusMessengerImpl<TMessage>(connectionString, factory.GetRequiredService<IApplicationLifetime>(), messageProcessor);
                        }
                        else
                        {
                            // AMQP 1.0

                            amqpMessenger = new AMQP10MessengerImpl<TMessage>(connectionString, factory.GetRequiredService<IApplicationLifetime>(), messageProcessor);
                        }

                        int[] amqpListenerCounts = new int[addressesSplit.Length];
                        
                        for (int i = 0; i < addressesSplit.Length; i++)
                        {
                            int amqpListenerCount = _configuration.GetValue<int>($"Amqp:{addressesSplit[i]}ListenerCount");
                            if (amqpListenerCount <= 0)
                                amqpListenerCount = 1;

                            amqpListenerCounts[i] = amqpListenerCount;
                        }

                        amqpMessenger.InitializeAddressesAsync(useAmqpListener, useAmqpSender, addressesSplit, amqpListenerCounts).Wait();
                        
                        return amqpMessenger;
                    });

                    Console.WriteLine("AMQP initialized successfully");
                });                
            }
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

            if (_useWebListener)
            {
                // see https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/httpsys

                // do not forget to install the SSL certificate before, see SETUP.txt
                _builder.UseHttpSys();
            }
            else
            {
                _builder.UseKestrel((builderContext, options) =>
                {
                    options.Configure(builderContext.Configuration.GetSection("Kestrel"));

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
                        }

                        if (useApi)
                        { 
                            int apiPort = _configuration.GetValue<int>("WebServer:ApiPort");

                            options.Listen(IPAddress.Any, apiPort, listenOptions =>
                                listenOptions.UseHttps(certificate));
                        }                        
                    }
                });
            }

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

                services.AddScoped<IRestSharpClientProvider, RestSharpClientProviderImpl>();
            });

            _builder.UseIISIntegration();

            List<string> urls = new List<string>();
            
            if (useWeb)
            {
                string webServerUrl = useHttps ? "https://" : "http://";

                string webDomain = _configuration["WebServer:WebDomain"];
                if (string.IsNullOrEmpty(webDomain))
                    throw new Exception("Web domain not found in application settings");

                int webPort = _configuration.GetValue<int>("WebServer:WebPort");

                webServerUrl += webDomain;
                webServerUrl += ":" + webPort;

                urls.Add(webServerUrl);
            }

            if (useApi)
            { 
                string apiServerUrl = useHttps ? "https://" : "http://";

                string apiDomain = _configuration["WebServer:ApiDomain"];
                if (string.IsNullOrEmpty(apiDomain))
                    throw new Exception("API domain not found in application settings");

                int apiPort = _configuration.GetValue<int>("WebServer:ApiPort");

                apiServerUrl += apiDomain;
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