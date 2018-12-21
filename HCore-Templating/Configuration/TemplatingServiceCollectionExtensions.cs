using System;
using System.IO;
using System.Runtime.InteropServices;
using HCore.Templating.Renderer;
using HCore.Templating.Renderer.Impl;
using jsreport.AspNetCore;
using jsreport.Local;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TemplatingServiceCollectionExtensions
    {
        public static IServiceCollection AddTemplating(this IServiceCollection services)
        {
            services.AddScoped<ITemplateRenderer, TemplateRendererImpl>();
           
            return services;
        }

        public static IServiceCollection AddPdfRendering(this IServiceCollection services)
        {
            services.AddJsReport(new LocalReporting()
                .UseBinary(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)  ?
                    jsreport.Binary.JsReportBinary.GetBinary() :
                    jsreport.Binary.OSX.JsReportBinary.GetBinary())
                // if needed there is also jsreport.Binary.Linux.JsReportBinary.GetBinary()
                .KillRunningJsReportProcesses()
                .AsUtility()
                .Create());

            return services;
        }
    }
}
