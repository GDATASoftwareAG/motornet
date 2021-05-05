using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Utilities.Abstractions;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Motor.Extensions.Diagnostics.Telemetry
{
    public static class TelemetryHostBuilderExtensions
    {
        public static readonly string JaegerExporter = "JaegerExporter";
        public static readonly string OtlpExporter = "OtlpExporter";
        public static readonly string OpenTelemetry = "OpenTelemetry";
        public static readonly string AttributeMotorNetEnvironment = "motor.net.environment";
        public static readonly string AttributeMotorNetLibraryVersion = "motor.net.libversion";
        private enum UsedExporter
        {
            Oltp,
            Jaeger
        }

        public static IMotorHostBuilder ConfigureOpenTelemetry(this IMotorHostBuilder hostBuilder)
        {
            hostBuilder
                .ConfigureServices((hostContext, services) =>
                {
                    var usedExport = UsedExporter.Jaeger;
                    if (hostContext.Configuration.GetSection(OtlpExporter).Exists())
                    {
                        services.Configure<OtlpExporterOptions>(hostContext.Configuration.GetSection(OtlpExporter));
                        usedExport = UsedExporter.Oltp;
                    }
                    else
                    {
                        services.Configure<JaegerExporterOptions>(hostContext.Configuration.GetSection(JaegerExporter));
                    }

                    var telemetryOptions = new OpenTelemetryOptions();
                    hostContext.Configuration.GetSection(OpenTelemetry).Bind(telemetryOptions);
                    services.AddSingleton(telemetryOptions);
                    services.AddOpenTelemetryTracing(builder =>
                    {
                        builder
                            .AddAspNetCoreInstrumentation(options =>
                            {
                                options.Filter = TelemetryRequestFilter(telemetryOptions);
                            })
                            .Configure((provider, providerBuilder) =>
                            {
                                var httpClientInstrumentationOptions =
                                    provider.GetService<IOptions<HttpClientInstrumentationOptions>>()?.Value ??
                                    new HttpClientInstrumentationOptions();
                                var applicationNameService = provider.GetRequiredService<IApplicationNameService>();
                                var logger = provider.GetRequiredService<ILogger<OpenTelemetryOptions>>();
                                providerBuilder = providerBuilder
                                    .AddHttpClientInstrumentation(options =>
                                    {
                                        options.Enrich = httpClientInstrumentationOptions.Enrich;
                                        options.Filter = httpClientInstrumentationOptions.Filter;
                                        options.SetHttpFlavor = httpClientInstrumentationOptions.SetHttpFlavor;
                                    })
                                    .AddSource(OpenTelemetryOptions.DefaultActivitySourceName)
                                    .AddSource(telemetryOptions.Sources.ToArray())
                                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                                        .AddService(applicationNameService.GetFullName(),
                                            serviceVersion: applicationNameService.GetVersion())
                                        .AddAttributes(new Dictionary<string, object>
                                        {
                                            {
                                                AttributeMotorNetEnvironment,
                                                hostContext.HostingEnvironment.EnvironmentName.ToLower()
                                            },
                                            {
                                                AttributeMotorNetLibraryVersion,
                                                applicationNameService.GetLibVersion()
                                            }
                                        }))
                                    .SetMotorSampler(telemetryOptions);

                                switch (usedExport)
                                {
                                    case UsedExporter.Oltp:
                                        providerBuilder.AddOtlpExporter(logger, provider);
                                        break;
                                    default:
                                        providerBuilder.AddJaegerExporter(logger, provider);
                                        break;
                                }
                            });
                    });
                });
            return hostBuilder;
        }


        private static Func<HttpContext, bool> TelemetryRequestFilter(OpenTelemetryOptions openTelemetryOptions) =>
            req => !openTelemetryOptions.FilterOutTelemetryRequests ||
                   !req.Request.Path.StartsWithSegments("/metrics") &&
                   !req.Request.Path.StartsWithSegments("/health");

        private static TracerProviderBuilder SetMotorSampler(this TracerProviderBuilder builder,
            OpenTelemetryOptions options)
        {
            Sampler sampler = options.SamplingProbability switch
            {
                > 0.9999 => new AlwaysOnSampler(),
                _ => new TraceIdRatioBasedSampler(options.SamplingProbability)
            };

            builder.SetSampler(new ParentBasedSampler(sampler));
            return builder;
        }

        private static void AddJaegerExporter(this TracerProviderBuilder builder, ILogger logger,
            IServiceProvider provider)
        {
            try
            {
                var options = provider.GetService<IOptions<JaegerExporterOptions>>()!.Value;
                Dns.GetHostEntry(options.AgentHost);
                builder.AddJaegerExporter(internalOptions =>
                {
                    internalOptions.AgentHost = options.AgentHost;
                    internalOptions.AgentPort = options.AgentPort;
                });
            }
            catch (Exception ex)
            {
                logger.LogWarning(LogEvents.JaegerConfigurationFailed, ex,
                    "Jaeger configuration failed.");
            }
        }

        private static void AddOtlpExporter(this TracerProviderBuilder builder, ILogger logger,
            IServiceProvider provider)
        {
            try
            {
                var options = provider.GetService<IOptions<OtlpExporterOptions>>()!.Value;
                Dns.GetHostEntry(options.Endpoint.Host);
                builder.AddOtlpExporter(internalOptions =>
                {
                    internalOptions.Endpoint = options.Endpoint;
                    internalOptions.Headers = options.Headers;
                    internalOptions.TimeoutMilliseconds = options.TimeoutMilliseconds;
                    internalOptions.ExportProcessorType = options.ExportProcessorType;
                    internalOptions.BatchExportProcessorOptions = options.BatchExportProcessorOptions;
                });
            }
            catch (Exception ex)
            {
                logger.LogWarning(LogEvents.JaegerConfigurationFailed, ex,
                    "Otlp configuration failed.");
            }
        }
    }
}
