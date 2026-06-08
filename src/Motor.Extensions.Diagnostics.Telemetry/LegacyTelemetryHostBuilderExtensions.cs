using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Hosting.CloudEvents;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Motor.Extensions.Diagnostics.Telemetry;

public static class LegacyTelemetryHostBuilderExtensions
{
    extension(IHostBuilder builder)
    {
        public IHostBuilder ConfigureOpenTelemetry()
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            Activity.ForceDefaultIdFormat = true;
            builder.ConfigureServices(
                (hostContext, services) =>
                {
                    UsedExporter? usedExport;
                    if (hostContext.Configuration.GetSection(TelemetryHostBuilderExtensions.OtlpExporter).Exists())
                    {
                        services.Configure<OtlpExporterOptions>(
                            hostContext.Configuration.GetSection(TelemetryHostBuilderExtensions.OtlpExporter)
                        );
                        usedExport = UsedExporter.Otlp;
                    }
                    else
                    {
                        services.Configure<JaegerExporterOptions>(
                            hostContext.Configuration.GetSection(TelemetryHostBuilderExtensions.JaegerExporter)
                        );
                        usedExport = UsedExporter.Jaeger;
                    }

                    var telemetryOptions = new OpenTelemetryOptions();
                    hostContext
                        .Configuration.GetSection(TelemetryHostBuilderExtensions.OpenTelemetry)
                        .Bind(telemetryOptions);
                    services.AddSingleton(telemetryOptions);
                    services
                        .AddOpenTelemetry()
                        .WithTracing(builder =>
                        {
                            builder
                                .AddAspNetCoreInstrumentation(options =>
                                {
                                    options.Filter = TelemetryRequestFilter(telemetryOptions);
                                })
                                .AddSource(OpenTelemetryOptions.DefaultActivitySourceName)
                                .AddSource(telemetryOptions.Sources.ToArray())
                                .SetMotorSampler(telemetryOptions)
                                .AddExporter(usedExport);
                            if (builder is IDeferredTracerProviderBuilder deferredTracerProviderBuilder)
                            {
                                deferredTracerProviderBuilder.Configure(
                                    (provider, providerBuilder) =>
                                    {
                                        providerBuilder.ConfigureResource(resourceBuilder =>
                                        {
                                            var applicationNameService =
                                                provider.GetRequiredService<IApplicationNameService>();
                                            resourceBuilder
                                                .AddAttributes(
                                                    new Dictionary<string, object>
                                                    {
                                                        {
                                                            TelemetryHostBuilderExtensions.AttributeMotorNetEnvironment,
                                                            hostContext.HostingEnvironment.EnvironmentName.ToLower()
                                                        },
                                                        {
                                                            TelemetryHostBuilderExtensions.AttributeMotorNetLibraryVersion,
                                                            applicationNameService.GetLibVersion()
                                                        },
                                                    }
                                                )
                                                .AddService(
                                                    applicationNameService.GetFullName(),
                                                    serviceVersion: applicationNameService.GetVersion()
                                                );
                                        });
                                    }
                                );
                            }
                        });
                }
            );
            return builder;
        }
    }

    private static Func<HttpContext, bool> TelemetryRequestFilter(OpenTelemetryOptions openTelemetryOptions) =>
        req =>
            !openTelemetryOptions.FilterOutTelemetryRequests
            || !req.Request.Path.StartsWithSegments("/metrics") && !req.Request.Path.StartsWithSegments("/health");

    private static TracerProviderBuilder SetMotorSampler(
        this TracerProviderBuilder builder,
        OpenTelemetryOptions options
    )
    {
        Sampler sampler = options.SamplingProbability switch
        {
            >= 1 => new AlwaysOnSampler(),
            _ => new TraceIdRatioBasedSampler(options.SamplingProbability),
        };

        builder.SetSampler(new ParentBasedSampler(sampler));
        return builder;
    }

    private static void AddExporter(this TracerProviderBuilder builder, UsedExporter? exporter)
    {
        switch (exporter)
        {
            case UsedExporter.Otlp:
                builder.AddOtlpExporter();
                break;
            case UsedExporter.Jaeger:
                builder.AddJaegerExporter();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(exporter), exporter, null);
        }
    }
}
