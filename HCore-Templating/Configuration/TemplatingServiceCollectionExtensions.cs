using System.Runtime.InteropServices;
using HCore.Templating.Renderer;
using HCore.Templating.Renderer.Impl;
using jsreport.AspNetCore;
using jsreport.Binary;
using jsreport.Local;
using jsreport.Shared;

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
                .KillRunningJsReportProcesses()
                .AsUtility()
                .Create());

            return services;
        }
    }
}
