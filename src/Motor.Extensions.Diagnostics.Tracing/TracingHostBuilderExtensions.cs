using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Utilities.Abstractions;
using OpenTelemetry;
using OpenTelemetry.Exporter.Jaeger;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Motor.Extensions.Diagnostics.Tracing
{
    public static class TracingHostBuilderExtensions
    {
        public static readonly string JaegerExporter = "JaegerExporter";
        public static readonly string OpenTelemetry = "OpenTelemetry";
        public static readonly string AttributeMotorNetEnvironment = "motor.net.environment";
        public static readonly string AttributeMotorNetLibraryVersion = "motor.net.libversion";

        public static IMotorHostBuilder ConfigureJaegerTracing(this IMotorHostBuilder hostBuilder)
        {
            hostBuilder
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<JaegerExporterOptions>(hostContext.Configuration.GetSection(JaegerExporter));
                    services.Configure<OpenTelemetryOptions>(hostContext.Configuration.GetSection(OpenTelemetry));
                    services.AddOpenTelemetryTracing((provider, builder) =>
                    {
                        var jaegerOptions = provider.GetService<IOptions<JaegerExporterOptions>>()
                                            ?? throw new InvalidConstraintException($"{nameof(JaegerExporterOptions)} is not configured.");
                        var openTelemetryOptions = provider.GetService<IOptions<OpenTelemetryOptions>>()?.Value
                                                   ?? new OpenTelemetryOptions();
                        var applicationNameService = provider.GetService<IApplicationNameService>()
                                                     ?? throw new InvalidConstraintException($"{nameof(IApplicationNameService)} is not configured.");
                        var logger = provider.GetService<ILogger<OpenTelemetryOptions>>()
                                     ?? throw new InvalidConstraintException($"{nameof(ILogger<OpenTelemetryOptions>)} is not configured.");

                        builder
                            .AddAspNetCoreInstrumentation()
                            .AddHttpClientInstrumentation()
                            .AddSource(openTelemetryOptions.Sources.ToArray())
                            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(applicationNameService.GetFullName(), serviceVersion: applicationNameService.GetVersion()))
                            .SetMotorSampler(openTelemetryOptions)
                            .AddExporter(logger, jaegerOptions.Value, applicationNameService, hostContext);
                    });
                });
            return hostBuilder;
        }

        private static TracerProviderBuilder SetMotorSampler(this TracerProviderBuilder builder,
            OpenTelemetryOptions options)
        {
            Sampler sampler = (Math.Abs(options.SamplingProbability - 1.0) < 0.0001)
                ? (Sampler)new AlwaysOnSampler()
                : new TraceIdRatioBasedSampler(options.SamplingProbability);

            builder.SetSampler(new ParentBasedSampler(sampler));
            return builder;
        }

        private static void AddExporter(this TracerProviderBuilder builder, ILogger logger,
            JaegerExporterOptions options, IApplicationNameService applicationNameService,
            HostBuilderContext hostContext)
        {
            try
            {
                Dns.GetHostEntry(options.AgentHost);
                builder.AddJaegerExporter(internalOptions =>
                {
                    var dictionary = options.ProcessTags?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? new Dictionary<string, object>();
                    dictionary.Add(AttributeMotorNetEnvironment, hostContext.HostingEnvironment.EnvironmentName.ToLower());
                    dictionary.Add(AttributeMotorNetLibraryVersion, applicationNameService.GetLibVersion());
                    internalOptions.ProcessTags = dictionary.ToList();
                    internalOptions.AgentHost = options.AgentHost;
                    internalOptions.AgentPort = options.AgentPort;
                });
            }
            catch (Exception ex)
            {
                logger.LogWarning(LogEvents.JaegerConfigurationFailed, ex, "Jaeger configuration failed, fallback to console.");
                builder.AddConsoleExporter();
            }
        }
    }
}
