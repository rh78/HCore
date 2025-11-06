using System;
using System.ComponentModel.DataAnnotations;
using HCore.Web.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace HCore.Web.Configuration
{
    public static class OpenTelemetryExtensions
    {
        private static OtlpExportProtocol? _protocol = null;

        public static IServiceCollection AddOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
        {
            var openTelemetryConfigurationModel = configuration.GetSection("OpenTelemetry").Get<OpenTelemetryConfigurationModel>();

            var validationContext = new ValidationContext(openTelemetryConfigurationModel);

            Validator.ValidateObject(openTelemetryConfigurationModel, validationContext, validateAllProperties: true);

            var openTelemetryBuilder = services
                .AddOpenTelemetry()
                .ConfigureResource(resourceBuilder => resourceBuilder.AddService(openTelemetryConfigurationModel.ServiceName, openTelemetryConfigurationModel.ServiceVersion));

            if (openTelemetryConfigurationModel.AddMetrics)
            {
                openTelemetryBuilder = openTelemetryBuilder.WithMetrics(meterProviderBuilder =>
                {
                    meterProviderBuilder.AddAspNetCoreInstrumentation();
                    meterProviderBuilder.AddHttpClientInstrumentation();
                    meterProviderBuilder.AddAWSInstrumentation();

                    meterProviderBuilder.AddOtlpExporter(otlpExporterOptions => SetOtlpExporterOptions(otlpExporterOptions, openTelemetryConfigurationModel));
                });
            }

            if (openTelemetryConfigurationModel.AddTracing)
            {
                openTelemetryBuilder = openTelemetryBuilder.WithTracing(tracerProviderBuilder =>
                {
                    tracerProviderBuilder.AddAspNetCoreInstrumentation();
                    tracerProviderBuilder.AddHttpClientInstrumentation();
                    tracerProviderBuilder.AddAWSInstrumentation();
                    tracerProviderBuilder.AddNpgsql();

                    tracerProviderBuilder.AddOtlpExporter(otlpExporterOptions => SetOtlpExporterOptions(otlpExporterOptions, openTelemetryConfigurationModel));
                });
            }

            if (openTelemetryConfigurationModel.AddLogging)
            {
                openTelemetryBuilder = openTelemetryBuilder.WithLogging(
                    loggerProviderBuilder =>
                    {
                        loggerProviderBuilder.AddOtlpExporter(otlpExporterOptions => SetOtlpExporterOptions(otlpExporterOptions, openTelemetryConfigurationModel));
                    },
                    openTelemetryLoggerOptions =>
                    {
                        openTelemetryLoggerOptions.IncludeFormattedMessage = true;
                        openTelemetryLoggerOptions.IncludeScopes = true;
                    });
            }

            return services;
        }

        private static void SetOtlpExporterOptions(OtlpExporterOptions otlpExporterOptions, OpenTelemetryConfigurationModel openTelemetryConfigurationModel)
        {
            if (_protocol == null)
            {
                // default is Grpc

                _protocol = openTelemetryConfigurationModel.Protocol switch
                {
                    "Grpc" => OtlpExportProtocol.Grpc,
                    "HttpProtobuf" => OtlpExportProtocol.HttpProtobuf,
                    _ => otlpExporterOptions.Protocol
                };
            }

            otlpExporterOptions.Protocol = _protocol.Value;

            if (!string.IsNullOrEmpty(openTelemetryConfigurationModel.Endpoint))
            {
                otlpExporterOptions.Endpoint = new Uri(openTelemetryConfigurationModel.Endpoint);
            }
        }
    }
}
