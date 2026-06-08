using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Diagnostics.Telemetry;
using Motor.Extensions.Hosting;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.HealthChecks;

namespace Motor.Extensions.Utilities;

public static class DefaultHostBuilderExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder ConfigureSingleOutputService<TInput, TOutput>(
            string healthCheckConfigSection = "HealthChecks",
            string messageProcessingHealthCheckName = nameof(MessageProcessingHealthCheck<TInput>),
            string tooManyTemporaryFailuresHealthCheckName = nameof(TooManyTemporaryFailuresHealthCheck<TInput>)
        )
            where TOutput : class
            where TInput : class
        {
            builder.ConfigureNoOutputService<TInput>(
                healthCheckConfigSection,
                messageProcessingHealthCheckName,
                tooManyTemporaryFailuresHealthCheckName
            );
            builder.Services.AddTransient<INoOutputService<TInput>, SingleOutputServiceAdapter<TInput, TOutput>>();

            return builder;
        }

        public IHostApplicationBuilder ConfigureMultiOutputService<TInput, TOutput>(
            string healthCheckConfigSection = "HealthChecks",
            string messageProcessingHealthCheckName = nameof(MessageProcessingHealthCheck<TInput>),
            string tooManyTemporaryFailuresHealthCheckName = nameof(TooManyTemporaryFailuresHealthCheck<TInput>)
        )
            where TOutput : class
            where TInput : class
        {
            builder.ConfigureNoOutputService<TInput>(
                healthCheckConfigSection,
                messageProcessingHealthCheckName,
                tooManyTemporaryFailuresHealthCheckName
            );
            builder.Services.AddTransient<INoOutputService<TInput>, MultiOutputServiceAdapter<TInput, TOutput>>();

            return builder;
        }

        public IHostApplicationBuilder ConfigureNoOutputService<TInput>(
            string healthCheckConfigSection = "HealthChecks",
            string messageProcessingHealthCheckName = nameof(MessageProcessingHealthCheck<TInput>),
            string tooManyTemporaryFailuresHealthCheckName = nameof(TooManyTemporaryFailuresHealthCheck<TInput>)
        )
            where TInput : class
        {
            builder
                .Services.AddHealthChecks()
                .AddCheck<MessageProcessingHealthCheck<TInput>>(messageProcessingHealthCheckName)
                .AddCheck<TooManyTemporaryFailuresHealthCheck<TInput>>(tooManyTemporaryFailuresHealthCheckName);
            builder.Services.AddQueuedGenericService<TInput>();
            builder.Services.AddTransient<
                DelegatingMessageHandler<TInput>,
                TelemetryDelegatingMessageHandler<TInput>
            >();
            builder.Services.AddTransient<
                DelegatingMessageHandler<TInput>,
                PrometheusDelegatingMessageHandler<TInput>
            >();
            builder.Services.Configure<MessageProcessingOptions>(
                builder.Configuration.GetSection(healthCheckConfigSection).GetSection(messageProcessingHealthCheckName)
            );
            builder.Services.AddSingleton<TooManyTemporaryFailuresStatistics<TInput>>();
            builder.Services.AddTransient<
                DelegatingMessageHandler<TInput>,
                TooManyTemporaryFailuresDelegatingMessageHandler<TInput>
            >();
            builder.Services.Configure<TooManyTemporaryFailuresOptions>(
                builder
                    .Configuration.GetSection(healthCheckConfigSection)
                    .GetSection(tooManyTemporaryFailuresHealthCheckName)
            );

            return builder;
        }
    }

    extension(IHostBuilder builder)
    {
        public IHostBuilder ConfigureSingleOutputService<TInput, TOutput>(
            string healthCheckConfigSection = "HealthChecks",
            string messageProcessingHealthCheckName = nameof(MessageProcessingHealthCheck<TInput>),
            string tooManyTemporaryFailuresHealthCheckName = nameof(TooManyTemporaryFailuresHealthCheck<TInput>)
        )
            where TOutput : class
            where TInput : class =>
            builder
                .ConfigureNoOutputService<TInput>(
                    healthCheckConfigSection,
                    messageProcessingHealthCheckName,
                    tooManyTemporaryFailuresHealthCheckName
                )
                .ConfigureServices(
                    (_, services) =>
                    {
                        services.AddTransient<INoOutputService<TInput>, SingleOutputServiceAdapter<TInput, TOutput>>();
                    }
                );

        public IHostBuilder ConfigureMultiOutputService<TInput, TOutput>(
            string healthCheckConfigSection = "HealthChecks",
            string messageProcessingHealthCheckName = nameof(MessageProcessingHealthCheck<TInput>),
            string tooManyTemporaryFailuresHealthCheckName = nameof(TooManyTemporaryFailuresHealthCheck<TInput>)
        )
            where TOutput : class
            where TInput : class =>
            builder
                .ConfigureNoOutputService<TInput>(
                    healthCheckConfigSection,
                    messageProcessingHealthCheckName,
                    tooManyTemporaryFailuresHealthCheckName
                )
                .ConfigureServices(
                    (_, services) =>
                    {
                        services.AddTransient<INoOutputService<TInput>, MultiOutputServiceAdapter<TInput, TOutput>>();
                    }
                );

        public IHostBuilder ConfigureNoOutputService<TInput>(
            string healthCheckConfigSection = "HealthChecks",
            string messageProcessingHealthCheckName = nameof(MessageProcessingHealthCheck<TInput>),
            string tooManyTemporaryFailuresHealthCheckName = nameof(TooManyTemporaryFailuresHealthCheck<TInput>)
        )
            where TInput : class =>
            builder.ConfigureServices(
                (hostContext, services) =>
                {
                    services
                        .AddHealthChecks()
                        .AddCheck<MessageProcessingHealthCheck<TInput>>(messageProcessingHealthCheckName)
                        .AddCheck<TooManyTemporaryFailuresHealthCheck<TInput>>(tooManyTemporaryFailuresHealthCheckName);
                    services.AddQueuedGenericService<TInput>();
                    services.AddTransient<
                        DelegatingMessageHandler<TInput>,
                        TelemetryDelegatingMessageHandler<TInput>
                    >();
                    services.AddTransient<
                        DelegatingMessageHandler<TInput>,
                        PrometheusDelegatingMessageHandler<TInput>
                    >();
                    services.Configure<MessageProcessingOptions>(
                        hostContext
                            .Configuration.GetSection(healthCheckConfigSection)
                            .GetSection(messageProcessingHealthCheckName)
                    );
                    services.AddSingleton<TooManyTemporaryFailuresStatistics<TInput>>();
                    services.AddTransient<
                        DelegatingMessageHandler<TInput>,
                        TooManyTemporaryFailuresDelegatingMessageHandler<TInput>
                    >();
                    services.Configure<TooManyTemporaryFailuresOptions>(
                        hostContext
                            .Configuration.GetSection(healthCheckConfigSection)
                            .GetSection(tooManyTemporaryFailuresHealthCheckName)
                    );
                }
            );
    }
}
