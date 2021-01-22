using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Utilities;
using Motor.Extensions.Utilities.Abstractions;

namespace Motor.Extensions.Hosting.Nano
{
    public static class NanoHostBuilderNoOutputExtensions
    {
        public static IMotorHostBuilder ConfigureAnonymousNoOutputService<TInput>(
            this IMotorHostBuilder hostBuilder,
            Func<MotorCloudEvent<TInput>, ProcessedMessageStatus> handler,
            string healthCheckConfigSection = "HealthCheck")
            where TInput : class
        {
            return hostBuilder
                .ConfigureNoOutputService<TInput>(healthCheckConfigSection)
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<INoOutputService<TInput>>(_ =>
                        new AnonymousNoOutputService<TInput>(handler));
                });
        }

        public static IMotorHostBuilder ConfigureAnonymousNoOutputService<TInput, TDep1>(
            this IMotorHostBuilder hostBuilder,
            Func<MotorCloudEvent<TInput>, TDep1, ProcessedMessageStatus> handler,
            string healthCheckConfigSection = "HealthCheck")
            where TInput : class
            where TDep1 : class
        {
            return hostBuilder
                .ConfigureNoOutputService<TInput>(healthCheckConfigSection)
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<INoOutputService<TInput>>(provider =>
                        new AnonymousNoOutputService<TInput>((cloudEvent) =>
                            handler(cloudEvent, provider.GetRequiredService<TDep1>())));
                });
        }

        public static IMotorHostBuilder ConfigureAnonymousNoOutputService<TInput, TDep1, TDep2>(
            this IMotorHostBuilder hostBuilder,
            Func<MotorCloudEvent<TInput>, TDep1, TDep2, ProcessedMessageStatus> handler,
            string healthCheckConfigSection = "HealthCheck")
            where TInput : class
            where TDep1 : class
            where TDep2 : class
        {
            return hostBuilder
                .ConfigureNoOutputService<TInput>(healthCheckConfigSection)
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<INoOutputService<TInput>>(provider =>
                        new AnonymousNoOutputService<TInput>((cloudEvent) => handler(cloudEvent,
                            provider.GetRequiredService<TDep1>(), provider.GetRequiredService<TDep2>())));
                });
        }

        public static IMotorHostBuilder ConfigureAnonymousNoOutputService<TInput, TDep1, TDep2, TDep3>(
            this IMotorHostBuilder hostBuilder,
            Func<MotorCloudEvent<TInput>, TDep1, TDep2, TDep3, ProcessedMessageStatus> handler,
            string healthCheckConfigSection = "HealthCheck")
            where TInput : class
            where TDep1 : class
            where TDep2 : class
            where TDep3 : class
        {
            return hostBuilder
                .ConfigureNoOutputService<TInput>(healthCheckConfigSection)
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<INoOutputService<TInput>>(provider =>
                        new AnonymousNoOutputService<TInput>((cloudEvent) => handler(cloudEvent,
                            provider.GetRequiredService<TDep1>(),
                            provider.GetRequiredService<TDep2>(),
                            provider.GetRequiredService<TDep3>()))
                    );
                });
        }

        public static IMotorHostBuilder ConfigureAnonymousNoOutputService<TInput>(
            this IMotorHostBuilder hostBuilder,
            Func<MotorCloudEvent<TInput>, CancellationToken, Task<ProcessedMessageStatus>> handler,
            string healthCheckConfigSection = "HealthCheck")
            where TInput : class
        {
            return hostBuilder
                .ConfigureNoOutputService<TInput>(healthCheckConfigSection)
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<INoOutputService<TInput>>(_ =>
                        new AnonymousNoOutputService<TInput>(handler));
                });
        }

        public static IMotorHostBuilder ConfigureAnonymousNoOutputService<TInput, TDep1>(
            this IMotorHostBuilder hostBuilder,
            Func<MotorCloudEvent<TInput>, TDep1, CancellationToken, Task<ProcessedMessageStatus>> handler,
            string healthCheckConfigSection = "HealthCheck")
            where TInput : class
            where TDep1 : class
        {
            return hostBuilder
                .ConfigureNoOutputService<TInput>(healthCheckConfigSection)
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<INoOutputService<TInput>>(provider =>
                        new AnonymousNoOutputService<TInput>((cloudEvent, token) =>
                            handler(cloudEvent, provider.GetRequiredService<TDep1>(), token)));
                });
        }

        public static IMotorHostBuilder ConfigureAnonymousNoOutputService<TInput, TDep1, TDep2>(
            this IMotorHostBuilder hostBuilder,
            Func<MotorCloudEvent<TInput>, TDep1, TDep2, CancellationToken, Task<ProcessedMessageStatus>> handler,
            string healthCheckConfigSection = "HealthCheck")
            where TInput : class
            where TDep1 : class
            where TDep2 : class
        {
            return hostBuilder
                .ConfigureNoOutputService<TInput>(healthCheckConfigSection)
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<INoOutputService<TInput>>(provider =>
                        new AnonymousNoOutputService<TInput>((cloudEvent, token) => handler(cloudEvent,
                            provider.GetRequiredService<TDep1>(), provider.GetRequiredService<TDep2>(), token)));
                });
        }

        public static IMotorHostBuilder ConfigureAnonymousNoOutputService<TInput, TDep1, TDep2, TDep3>(
            this IMotorHostBuilder hostBuilder,
            Func<MotorCloudEvent<TInput>, TDep1, TDep2, TDep3, CancellationToken, Task<ProcessedMessageStatus>> handler,
            string healthCheckConfigSection = "HealthCheck")
            where TInput : class
            where TDep1 : class
            where TDep2 : class
            where TDep3 : class
        {
            return hostBuilder
                .ConfigureNoOutputService<TInput>(healthCheckConfigSection)
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<INoOutputService<TInput>>(provider =>
                        new AnonymousNoOutputService<TInput>((cloudEvent, token) => handler(cloudEvent,
                            provider.GetRequiredService<TDep1>(),
                            provider.GetRequiredService<TDep2>(),
                            provider.GetRequiredService<TDep3>(), token))
                    );
                });
        }
    }
}
