using System.Runtime.InteropServices;
using HCore.Templating.Configuration;
using HCore.Templating.Renderer;
using HCore.Templating.Renderer.Impl;
using jsreport.AspNetCore;
using jsreport.Binary;
using jsreport.Local;
using jsreport.Shared;
using jsreport.Types;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TemplatingServiceCollectionExtensions
    {
        public static IServiceCollection AddTemplating(this IServiceCollection services)
        {
            services.AddScoped<ITemplateRenderer, TemplateRendererImpl>();
           
            return services;
        }

        public static IServiceCollection AddPdfRendering(this IServiceCollection services, string temporaryFolder)
        {
            if (string.IsNullOrEmpty(temporaryFolder))
            {
                throw new System.Exception("Temporary folder is missing");
            }

            temporaryFolder = $"{temporaryFolder}/jsreport";

            IReportingBinary binary = null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                binary = JsReportBinary.GetBinary();
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                binary = jsreport.Binary.OSX.JsReportBinary.GetBinary();
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                binary = jsreport.Binary.Linux.JsReportBinary.GetBinary();
            }

            services.AddJsReport(new LocalReporting()
                .TempDirectory(temporaryFolder)
                .UseBinary(binary)
                .Configure(configuration =>
                {
                    if (configuration.Chrome == null)
                    {
                        configuration.Chrome = new ChromeConfiguration();
                    }

                    configuration.Chrome = new HackedChromeConfiguration()
                    {
                        NumberOfWorkers = configuration.Chrome.NumberOfWorkers,
                        Strategy = configuration.Chrome.Strategy,
                        Timeout = configuration.Chrome.Timeout,
                        LaunchOptions = new HackedChromeConfiguration.ChromeLaunchOptionsConfiguration()
                        {
                            Args = "--no-sandbox"
                        }
                    };

                    return configuration;
                })
                .KillRunningJsReportProcesses()
                .AsUtility()
                .Create()); ;

            return services;
        }
    }
}
