using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Utilities.Abstractions;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Motor.Extensions.Diagnostics.Telemetry;

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
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;
        hostBuilder.ConfigureServices((hostContext, services) =>
        {
            UsedExporter? usedExport;
            if (hostContext.Configuration.GetSection(OtlpExporter).Exists())
            {
                services.Configure<OtlpExporterOptions>(hostContext.Configuration.GetSection(OtlpExporter));
                usedExport = UsedExporter.Oltp;
            }
            else
            {
                services.Configure<JaegerExporterOptions>(hostContext.Configuration.GetSection(JaegerExporter));
                usedExport = UsedExporter.Jaeger;
            }

            var telemetryOptions = new OpenTelemetryOptions();
            hostContext.Configuration.GetSection(OpenTelemetry).Bind(telemetryOptions);
            services.AddSingleton(telemetryOptions);
            services.AddOpenTelemetry().WithTracing(builder =>
            {
                builder.AddAspNetCoreInstrumentation(options =>
                    {
                        options.Filter = TelemetryRequestFilter(telemetryOptions);
                    })
                    .AddSource(OpenTelemetryOptions.DefaultActivitySourceName)
                    .AddSource(telemetryOptions.Sources.ToArray())
                    .SetMotorSampler(telemetryOptions)
                    .AddExporter(usedExport);
                if (builder is IDeferredTracerProviderBuilder deferredTracerProviderBuilder)
                {
                    deferredTracerProviderBuilder.Configure((provider, providerBuilder) =>
                    {
                        providerBuilder.ConfigureResource(resourceBuilder =>
                        {
                            var applicationNameService = provider.GetRequiredService<IApplicationNameService>();
                            resourceBuilder.AddAttributes(new Dictionary<string, object>
                                {
                                    {
                                        AttributeMotorNetEnvironment,
                                        hostContext.HostingEnvironment.EnvironmentName.ToLower()
                                    },
                                    {
                                        AttributeMotorNetLibraryVersion,
                                        applicationNameService.GetLibVersion()
                                    }
                                })
                                .AddService(applicationNameService.GetFullName(),
                                    serviceVersion: applicationNameService.GetVersion());
                        });
                    });
                }
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
            >= 1 => new AlwaysOnSampler(),
            _ => new TraceIdRatioBasedSampler(options.SamplingProbability)
        };

        builder.SetSampler(new ParentBasedSampler(sampler));
        return builder;
    }

    private static void AddExporter(this TracerProviderBuilder builder, UsedExporter? exporter)
    {
        switch (exporter)
        {
            case UsedExporter.Oltp:
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
