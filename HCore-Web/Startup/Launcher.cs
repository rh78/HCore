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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Hosting;
using System.Net.Security;
using HCore.Web.Providers;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;

namespace HCore.Web.Startup
{
    // Inspired from https://github.com/aspnet/MetaPackages/blob/2.1.3/src/Microsoft.AspNetCore/WebHost.cs

    public class Launcher<TStartup>
    {
        // see https://github.com/dotnet/corefx/issues/40830

#pragma warning disable CA1416 // Validate platform compatibility
        private static readonly CipherSuitesPolicy CipherSuitesPolicy = new CipherSuitesPolicy
        (
            new TlsCipherSuite[]
            {
                // Cipher suits as recommended by: https://wiki.mozilla.org/Security/Server_Side_TLS
                // Listed in preferred order.

                // From: https://en.internet.nl
                // High
                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256,
                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256,
                TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
                TlsCipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256,
                TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
                // Medium
                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA,
                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256,
                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA,
                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384,
                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256,
                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256,
                TlsCipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256,
                TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
                TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
                TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384,
                TlsCipherSuite.TLS_DHE_RSA_WITH_CHACHA20_POLY1305_SHA256,
                TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256,
                TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256,
                TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA256,
                TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA,
                TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA256,
                TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA,
                TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384,
                TlsCipherSuite.TLS_AES_128_GCM_SHA256,
                TlsCipherSuite.TLS_AES_256_GCM_SHA384,
                TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256,

                // for IE 11 on Win7
                TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384,
            }
        );
#pragma warning restore CA1416 // Validate platform compatibility

        private string _environment;

        private HostBuilder _hostBuilder;
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

            var host = _hostBuilder.Build();

            host.Run();
        }

        private void CreateWebHostBuilder()
        {
            _hostBuilder = new HostBuilder();

            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile($"appsettings.{_environment}.json", optional: true, reloadOnChange: false)
                .AddJsonFile($"appsettings.{_environment}.local.json", optional: true, reloadOnChange: false)
                .Build();

            ConfigureDefaultServiceProvider();
            ConfigureLogging();
            ConfigureConfiguration();

            _hostBuilder.UseContentRoot(Directory.GetCurrentDirectory());

            _hostBuilder.ConfigureWebHostDefaults(webHostBuilder =>
            {
                ConfigureContentRoot(webHostBuilder);
                ConfigureWebServer(webHostBuilder);
            });
        }

        private void ConfigureDefaultServiceProvider()
        {
            _hostBuilder.UseDefaultServiceProvider((context, options) =>
            {
                options.ValidateScopes = true;
            });
        }

        private void ConfigureLogging()
        {
            _hostBuilder.ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
                logging.AddDebug();
            });
        }

        private void ConfigureContentRoot(IWebHostBuilder webHostBuilder)
        {
            if (string.IsNullOrEmpty(webHostBuilder.GetSetting(WebHostDefaults.ContentRootKey)))
            {
                webHostBuilder.UseContentRoot(Directory.GetCurrentDirectory());
            }
        }

        private void ConfigureConfiguration()
        {
            _hostBuilder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                var env = hostingContext.HostingEnvironment;

                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                      .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false)
                      .AddJsonFile($"appsettings.{env.EnvironmentName}.local.json", optional: true, reloadOnChange: false);

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

        private void ConfigureWebServer(IWebHostBuilder webHostBuilder)
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

            Console.WriteLine($"Default connection limit: {ServicePointManager.DefaultConnectionLimit}");

            int availableWorkerThreads, availableWorkerIoThreads;

            ThreadPool.GetAvailableThreads(out availableWorkerThreads, out availableWorkerIoThreads);

            Console.WriteLine($"Available threads: {availableWorkerThreads}/{availableWorkerThreads}");

            webHostBuilder.UseShutdownTimeout(TimeSpan.FromMinutes(10));
            
            webHostBuilder.UseKestrel((builderContext, options) =>
            {
                X509Certificate2 defaultWebCertificate = null;
                X509Certificate2 defaultSecondaryWebCertificate = null;

                X509Certificate2 defaultApiCertificate = null;
                X509Certificate2 defaultSecondaryApiCertificate = null;

                string hostPattern = null;
                string secondaryHostPattern = null;

                if (useHttps)
                {
                    if (useWeb)
                    {
                        defaultWebCertificate = GetX509Certificate2("Web", isRequired: true);
                        defaultSecondaryWebCertificate = GetX509Certificate2("SecondaryWeb", isRequired: false);
                    }

                    if (useApi)
                    {
                        defaultApiCertificate = GetX509Certificate2("Api", isRequired: true);
                        defaultSecondaryApiCertificate = GetX509Certificate2("SecondaryApi", isRequired: false);
                    }

                    if (defaultWebCertificate != null || defaultApiCertificate != null)
                    {
                        hostPattern = _configuration["WebServer:HostPattern"];

                        if (string.IsNullOrEmpty(hostPattern))
                            hostPattern = ".smint.io";
                    }

                    if (defaultSecondaryWebCertificate != null || defaultSecondaryApiCertificate != null)
                    {
                        secondaryHostPattern = _configuration["WebServer:SecondaryHostPattern"];

                        if (string.IsNullOrEmpty(secondaryHostPattern))
                            throw new Exception("Secondary host pattern not found");
                    }
                }

                options.AddServerHeader = false;

                options.Configure(builderContext.Configuration.GetSection("Kestrel"));
                
                // try to avoid thread starvation problems

                // see https://www.strathweb.com/2019/02/be-careful-when-manually-handling-json-requests-in-asp-net-core/

                // see https://www.monitis.com/blog/improving-asp-net-performance-part3-threading/

                options.Limits.MaxConcurrentConnections = 24 * numberOfCores;
                options.Limits.MaxConcurrentUpgradedConnections = 24 * numberOfCores;

                if (maxRequestBodySizeKB > 0)
                    options.Limits.MaxRequestBodySize = maxRequestBodySizeKB * 1024;

                if (useHttps)
                {
                    // Important: Only enable TLS 1.2 and TLS 1.3 to comply with SSL Server tests.
                    //            TLS 1.1, TLS 1.0 and SSLv3 are considered insecure by todays standards.

                    options.ConfigureHttpsDefaults(httpsOptions =>
                    {
                        httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;

                        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                        {
                            var allowedCipherSuites = CipherSuitesPolicy.AllowedCipherSuites;

                            foreach (var allowedCipherSuite in allowedCipherSuites)
                            {
                                Console.WriteLine($"Cipher suite supported: {allowedCipherSuite}");
                            }

                            httpsOptions.OnAuthenticate = (conContext, sslAuthOptions) =>
                            {
                                // not supported if OpenSSL 1.1.1 is not present!
                                sslAuthOptions.CipherSuitesPolicy = CipherSuitesPolicy;
                            };
                        }

                        int? webPort = null;

                        if (useWeb)
                        {
                            webPort = _configuration.GetValue<int>("WebServer:WebPort");
                        }

                        int? apiPort = null;

                        if (useApi)
                        {
                            apiPort = _configuration.GetValue<int>("WebServer:ApiPort");
                        }

                        httpsOptions.ServerCertificateSelector = (connectionContext, hostName) =>
                        {
                            var port = ((IPEndPoint)connectionContext.LocalEndPoint).Port;

                            if (string.IsNullOrEmpty(hostName) ||
                                hostName.EndsWith(hostPattern))
                            {
                                // this is our default certificates

                                if (useWeb && port == webPort && defaultWebCertificate != null)
                                {
                                    return defaultWebCertificate;
                                }
                                else if (useApi && port == apiPort && defaultApiCertificate != null)
                                {
                                    return defaultApiCertificate;
                                }
                                else
                                {
                                    throw new Exception($"Default certificate not found ({hostName} / {useWeb} / {useApi} / {port} / {webPort} / {apiPort})");
                                }
                            }
                            else if ((defaultSecondaryWebCertificate != null || defaultSecondaryApiCertificate != null) && hostName.EndsWith(secondaryHostPattern))
                            {
                                if (useWeb && port == webPort && defaultSecondaryWebCertificate != null)
                                {
                                    return defaultSecondaryWebCertificate;
                                }
                                else if (useApi && port == apiPort && defaultSecondaryApiCertificate != null)
                                {
                                    return defaultSecondaryApiCertificate;
                                }
                                else
                                {
                                    throw new Exception($"Default certificate not found ({hostName} / {useWeb} / {useApi} / {port} / {webPort} / {apiPort})");
                                }
                            }

                            // this is some external certificates coming e.g. from the tenant database

                            var serviceProvider = options.ApplicationServices;

                            var serverCertificateSelector = serviceProvider.GetService<IServerCertificateSelector>();

                            if (serverCertificateSelector != null)
                            {
                                var customCertificate = serverCertificateSelector.GetServerCertificate(hostName);

                                if (customCertificate == null)
                                    throw new Exception($"Custom certificate not found ({hostName} / {useWeb} / {useApi} / {port} / {webPort} / {apiPort})");

                                return customCertificate;
                            }

                            // default fallback

                            if (useWeb && port == webPort && defaultWebCertificate != null)
                            {
                                return defaultWebCertificate;
                            }
                            else if (useApi && port == apiPort && defaultApiCertificate != null)
                            {
                                return defaultApiCertificate;
                            }
                            else
                            {
                                throw new Exception($"Fallback certificate not found ({hostName} / {useWeb} / {useApi} / {port} / {webPort} / {apiPort})");
                            }
                        };
                    });

                    if (useWeb)
                    {
                        int webPort = _configuration.GetValue<int>("WebServer:WebPort");

                        options.Listen(IPAddress.Any, webPort, listenOptions =>
                            listenOptions.UseHttps());

                        int httpHealthCheckPort = _configuration.GetValue<int>("WebServer:HttpHealthCheckPort");

                        if (httpHealthCheckPort > 0)
                        {
                            options.Listen(IPAddress.Any, httpHealthCheckPort);
                        }
                        else
                        {
                            // we can not do redirects to HTTPS if we have a health check running

                            int redirectHttpToHttpsWebPort = _configuration.GetValue<int>("WebServer:RedirectHttpToHttpsWebPort");

                            if (redirectHttpToHttpsWebPort > 0)
                                options.Listen(IPAddress.Any, redirectHttpToHttpsWebPort);
                        }
                    }

                    if (useApi)
                    {

                        int apiPort = _configuration.GetValue<int>("WebServer:ApiPort");

                        options.Listen(IPAddress.Any, apiPort, listenOptions =>
                            listenOptions.UseHttps());
                    }
                }
            });

            webHostBuilder.ConfigureServices((hostingContext, services) =>
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

            webHostBuilder.UseIISIntegration();

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
                    int httpHealthCheckPort = _configuration.GetValue<int>("WebServer:HttpHealthCheckPort");

                    if (httpHealthCheckPort > 0)
                    {
                        webServerUrl = "http://";

                        webServerUrl += urlPattern;
                        webServerUrl += ":" + httpHealthCheckPort;

                        urls.Add(webServerUrl);
                    }
                    else
                    {
                        // we can not do redirects to HTTPS if we have a health check running

                        int redirectHttpToHttpsWebPort = _configuration.GetValue<int>("WebServer:RedirectHttpToHttpsWebPort");

                        if (redirectHttpToHttpsWebPort > 0)
                        {
                            webServerUrl = "http://";

                            webServerUrl += urlPattern;
                            webServerUrl += ":" + redirectHttpToHttpsWebPort;

                            urls.Add(webServerUrl);
                        }
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

            webHostBuilder.UseUrls(urlsArray);

            webHostBuilder.UseStartup(typeof(TStartup));

            if (_configuration.GetValue<bool>("Sentry:UseSentry"))
                webHostBuilder.UseSentry();

            _serverUrl = string.Join(" / ", urlsArray);
        }

        private X509Certificate2 GetX509Certificate2(string key, bool isRequired)
        {
            string httpsCertificateAssembly = _configuration[$"WebServer:Https:Certificates:{key}:Assembly"];
            if (string.IsNullOrEmpty(httpsCertificateAssembly))
            {
                if (!isRequired)
                {
                    return null;
                }

                throw new Exception("HTTPS web certificate assembly not found");
            }

            string httpsCertificateName = _configuration[$"WebServer:Https:Certificates:{key}:Name"];

            if (string.IsNullOrEmpty(httpsCertificateName))
                throw new Exception("HTTPS web certificate name not found");

            string httpsCertificatePassword = _configuration[$"WebServer:Https:Certificates:{key}:Password"];

            if (string.IsNullOrEmpty(httpsCertificatePassword))
                throw new Exception("HTTPS web certificate password not found");

            // from https://stackoverflow.com/questions/50708394/read-embedded-file-from-resource-in-asp-net-core

            Assembly httpsAssembly = AppDomain.CurrentDomain.GetAssemblies().
                SingleOrDefault(assembly => assembly.GetName().Name == httpsCertificateAssembly);

            if (httpsAssembly == null)
                throw new Exception("HTTPS web certificate assembly is not present in the list of assemblies");

            var resourceStream = httpsAssembly.GetManifestResourceStream(httpsCertificateName);

            if (resourceStream == null)
                throw new Exception("HTTPS web certificate resource not found");

            return GetX509Certificate2(resourceStream, httpsCertificatePassword);
        }

        private static X509Certificate2 GetX509Certificate2(Stream stream, string password)
        {
            X509Certificate2 x509Certificate2;

            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            {
                var store = new Pkcs12StoreBuilder().Build();

                using (stream)
                    store.Load(stream, password.ToArray());

                var keyAlias = store.Aliases.Cast<string>().SingleOrDefault(a => store.IsKeyEntry(a));

                var key = (RsaPrivateCrtKeyParameters)store.GetKey(keyAlias).Key;

                var parameters = DotNetUtilities.ToRSAParameters(key);

                var rsa = new RSACryptoServiceProvider();

                rsa.ImportParameters(parameters);

                var bouncyCertificate = store.GetCertificate(keyAlias).Certificate;

                x509Certificate2 = new X509Certificate2(DotNetUtilities.ToX509Certificate(bouncyCertificate));
                x509Certificate2 = RSACertificateExtensions.CopyWithPrivateKey(x509Certificate2, rsa);
            }
            else
            {
                using (var memoryStream = new MemoryStream((int)stream.Length))
                {
                    stream.CopyTo(memoryStream);

                    x509Certificate2 = new X509Certificate2(memoryStream.ToArray(), password);
                }
            }

            return x509Certificate2;
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