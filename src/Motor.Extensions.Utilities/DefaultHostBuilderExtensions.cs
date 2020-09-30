using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Diagnostics.Tracing;
using Motor.Extensions.Hosting;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.HealthChecks;
using Motor.Extensions.Utilities.Abstractions;

namespace Motor.Extensions.Utilities
{
    public static class DefaultHostBuilderExtensions
    {
        public static IMotorHostBuilder ConfigureServices(this IMotorHostBuilder hostBuilder,
            Action<IServiceCollection> configureDelegate)
        {
            hostBuilder.ConfigureServices((context, collection) => configureDelegate(collection));
            return hostBuilder;
        }

        public static IMotorHostBuilder ConfigureAppConfiguration(this IMotorHostBuilder hostBuilder,
            Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            hostBuilder.ConfigureAppConfiguration(configureDelegate);
            return hostBuilder;
        }

        public static IMotorHostBuilder ConfigureSingleOutputService<TInput, TOutput>(
            this IMotorHostBuilder hostBuilder)
            where TOutput : class
            where TInput : class
        {
            return hostBuilder
                .ConfigureNoOutputService<TInput>()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<INoOutputService<TInput>, SingleOutputServiceAdapter<TInput, TOutput>>();
                });
        }

        public static IMotorHostBuilder ConfigureMultiOutputService<TInput, TOutput>(
            this IMotorHostBuilder hostBuilder)
            where TOutput : class
            where TInput : class
        {
            return hostBuilder
                .ConfigureNoOutputService<TInput>()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<INoOutputService<TInput>, MultiOutputServiceAdapter<TInput, TOutput>>();
                });
        }

        public static IMotorHostBuilder ConfigureNoOutputService<TInput>(this IMotorHostBuilder hostBuilder,
            string healthCheckConfigSection = "HealthCheck") where TInput : class
        {
            return hostBuilder
                .AddHealthCheck<MessageProcessingHealthCheck<TInput>>(nameof(MessageProcessingHealthCheck<TInput>))
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddQueuedGenericService<TInput>();
                    services.AddTransient<DelegatingMessageHandler<TInput>, TracingDelegatingMessageHandler<TInput>>();
                    services.Configure<MessageProcessingHealthCheckConfig>(
                        hostContext.Configuration.GetSection(healthCheckConfigSection));
                    services
                        .AddTransient<DelegatingMessageHandler<TInput>, PrometheusDelegatingMessageHandler<TInput>>();
                });
        }
    }
}
