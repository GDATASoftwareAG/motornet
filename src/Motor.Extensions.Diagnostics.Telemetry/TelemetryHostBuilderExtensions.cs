using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Utilities.Abstractions;
using OpenTelemetry.Exporter;
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

        public static IMotorHostBuilder ConfigureOpenTelemetry(this IMotorHostBuilder hostBuilder)
        {
            hostBuilder
                .ConfigureServices((hostContext, services) =>
                {
                    var otelSection = hostContext.Configuration.GetSection(OtlpExporter);
                    var jaegerSection = hostContext.Configuration.GetSection(JaegerExporter);
                    if (otelSection != null)
                    {
                        services.Configure<OtlpExporterOptions>(otelSection);
                    }
                    else if (jaegerSection != null)
                    {
                        services.Configure<JaegerExporterOptions>(jaegerSection);
                    }

                    if (jaegerSection == null && otelSection == null)
                    {
                        return;
                    }

                    services.Configure<OpenTelemetryOptions>(hostContext.Configuration.GetSection(OpenTelemetry));
                    services.AddOpenTelemetryTracing(builder =>
                    {
                        builder
                            .Configure((provider, providerBuilder) =>
                            {
                                var telemetryOptions =
                                    provider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;
                                var httpClientInstrumentationOptions =
                                    provider.GetService<IOptions<HttpClientInstrumentationOptions>>()?.Value ??
                                    new HttpClientInstrumentationOptions();
                                var applicationNameService = provider.GetRequiredService<IApplicationNameService>();
                                var logger = provider.GetRequiredService<ILogger<OpenTelemetryOptions>>();
                                providerBuilder = providerBuilder
                                    .AddAspNetCoreInstrumentation(options =>
                                    {
                                        options.Filter = TelemetryRequestFilter(telemetryOptions);
                                    })
                                    .AddHttpClientInstrumentation(options =>
                                    {
                                        options.Enrich = httpClientInstrumentationOptions.Enrich;
                                        options.Filter = httpClientInstrumentationOptions.Filter;
                                        options.SetHttpFlavor = httpClientInstrumentationOptions.SetHttpFlavor;
                                    })
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
                                if (otelSection != null)
                                {
                                    providerBuilder.AddOtlpExporter(logger, provider);
                                }
                                else
                                {
                                    providerBuilder.AddJaegerExporter(logger, provider);
                                }
                            })
                            .AddSource(OpenTelemetryOptions.DefaultActivitySourceName);
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
