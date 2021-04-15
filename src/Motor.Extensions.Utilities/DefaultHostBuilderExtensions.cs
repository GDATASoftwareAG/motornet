using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Diagnostics.Telemetry;
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
            hostBuilder.ConfigureServices((_, collection) => configureDelegate(collection));
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
                .ConfigureServices((_, services) =>
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
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<INoOutputService<TInput>, MultiOutputServiceAdapter<TInput, TOutput>>();
                });
        }

        public static IMotorHostBuilder ConfigureNoOutputService<TInput>(this IMotorHostBuilder hostBuilder,
            string healthCheckConfigSection = "HealthChecks",
            string messageProcessingHealthCheckName = nameof(MessageProcessingHealthCheck<TInput>),
            string tooManyTemporaryFailuresHealthCheckName = nameof(TooManyTemporaryFailuresHealthCheck<TInput>))
            where TInput : class
        {
            return hostBuilder
                .AddHealthCheck<MessageProcessingHealthCheck<TInput>>(messageProcessingHealthCheckName)
                .AddHealthCheck<TooManyTemporaryFailuresHealthCheck<TInput>>(tooManyTemporaryFailuresHealthCheckName)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddQueuedGenericService<TInput>();
                    services.AddTransient<DelegatingMessageHandler<TInput>, TelemetryDelegatingMessageHandler<TInput>>();
                    services
                        .AddTransient<DelegatingMessageHandler<TInput>, PrometheusDelegatingMessageHandler<TInput>>();
                    services.Configure<MessageProcessingOptions>(
                        hostContext.Configuration.GetSection(healthCheckConfigSection)
                            .GetSection(messageProcessingHealthCheckName));
                    services.AddSingleton<TooManyTemporaryFailuresStatistics<TInput>>();
                    services
                        .AddTransient<DelegatingMessageHandler<TInput>,
                            TooManyTemporaryFailuresDelegatingMessageHandler<TInput>>();
                    services.Configure<TooManyTemporaryFailuresOptions>(
                        hostContext.Configuration.GetSection(healthCheckConfigSection)
                            .GetSection(tooManyTemporaryFailuresHealthCheckName));
                });
        }
    }
}
