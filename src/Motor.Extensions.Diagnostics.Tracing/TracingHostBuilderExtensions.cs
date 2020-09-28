using System;
using System.Net;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Utilities.Abstractions;
using Jaeger;
using Jaeger.Reporters;
using Jaeger.Samplers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Motor.Extensions.Diagnostics.Tracing.Config;
using OpenTracing;
using OpenTracing.Tag;
using Jaeger.Senders.Thrift;

namespace Motor.Extensions.Diagnostics.Tracing
{
    public static class TracingHostBuilderExtensions
    {
        public static IMotorHostBuilder ConfigureJaegerTracing(this IMotorHostBuilder hostBuilder)
        {
            hostBuilder
                .ConfigureServices((hostContext, services) =>
                {
                    var tracingConfig = new TracingConfig();
                    hostContext.Configuration.GetSection("Tracing").Bind(tracingConfig);
                    services.AddSingleton<ITracer>(provider =>
                    {
                        var loggerFactory = provider.GetService<ILoggerFactory>();
                        var applicationNameService = provider.GetService<IApplicationNameService>();

                        var sampler = CreateSampler(tracingConfig);
                        var reporter = CreateReporter(loggerFactory, tracingConfig);

                        var serviceName = applicationNameService.GetFullName();

                        return new Tracer.Builder(serviceName)
                            .WithLoggerFactory(loggerFactory)
                            .WithReporter(reporter)
                            .WithSampler(sampler)
                            .WithTag(Tags.Component.Key, applicationNameService.GetProduct())
                            .WithTag("environment", hostContext.HostingEnvironment.EnvironmentName.ToLower())
                            .Build();
                    });
                });
            return hostBuilder;
        }

        private static ISampler CreateSampler(TracingConfig tracingConfig)
        {
            if (Math.Abs(tracingConfig.SamplingProbability - 1.0) < 0.0001)
            {
                return new ConstSampler(true);
            }

            return new ProbabilisticSampler(tracingConfig.SamplingProbability);
        }

        private static IReporter CreateReporter(ILoggerFactory loggerFactory, TracingConfig tracingConfig)
        {
            try
            {
                Dns.GetHostEntry(tracingConfig.AgentHost);
                return new RemoteReporter.Builder()
                    .WithLoggerFactory(loggerFactory)
                    .WithSender(new UdpSender(tracingConfig.AgentHost, tracingConfig.AgentPort,
                        tracingConfig.ReporterMaxPacketSize))
                    .Build();
            }
            catch (Exception)
            {
                return new LoggingReporter(loggerFactory);
            }
        }
    }
}
