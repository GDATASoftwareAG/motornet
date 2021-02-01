using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Utilities;
using Motor.Extensions.Utilities.Abstractions;

namespace Motor.Extensions.Hosting.Nano
{
    public static class NanoHostBuilderSingleOutputExtensions
    {
        public static IMotorHostBuilder ConfigureAnonymousSingleOutputService<TInput, TOutput>(
            this IMotorHostBuilder hostBuilder,
            Func<MotorCloudEvent<TInput>, MotorCloudEvent<TOutput>> handler)
            where TInput : class
            where TOutput : class
        {
            return hostBuilder
                .ConfigureSingleOutputService<TInput, TOutput>()
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<ISingleOutputService<TInput, TOutput>>(_ =>
                        new AnonymousSingleOutputService<TInput, TOutput>(handler)
                    );
                });
        }

        public static IMotorHostBuilder ConfigureAnonymousSingleOutputService<TInput, TOutput, TDep1>(
            this IMotorHostBuilder hostBuilder,
            Func<MotorCloudEvent<TInput>, TDep1, MotorCloudEvent<TOutput>> handler)
            where TInput : class
            where TOutput : class
            where TDep1 : class
        {
            return hostBuilder
                .ConfigureSingleOutputService<TInput, TOutput>()
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<ISingleOutputService<TInput, TOutput>>(
                        provider =>
                            new AnonymousSingleOutputService<TInput, TOutput>(cloudEvent =>
                                handler(cloudEvent, provider.GetRequiredService<TDep1>())
                            )
                    );
                });
        }

        public static IMotorHostBuilder ConfigureAnonymousSingleOutputService<TInput, TOutput, TDep1, TDep2>(
            this IMotorHostBuilder hostBuilder,
            Func<MotorCloudEvent<TInput>, TDep1, TDep2, MotorCloudEvent<TOutput>> handler)
            where TInput : class
            where TOutput : class
            where TDep1 : class
            where TDep2 : class
        {
            return hostBuilder
                .ConfigureSingleOutputService<TInput, TOutput>()
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<ISingleOutputService<TInput, TOutput>>(
                        provider =>
                            new AnonymousSingleOutputService<TInput, TOutput>(cloudEvent =>
                                handler(cloudEvent, provider.GetRequiredService<TDep1>(), provider.GetRequiredService<TDep2>())
                            )
                    );
                });
        }

        public static IMotorHostBuilder ConfigureAnonymousSingleOutputService<TInput, TOutput, TDep1, TDep2, TDep3>(
            this IMotorHostBuilder hostBuilder,
            Func<MotorCloudEvent<TInput>, TDep1, TDep2, TDep3, MotorCloudEvent<TOutput>> handler)
            where TInput : class
            where TOutput : class
            where TDep1 : class
            where TDep2 : class
            where TDep3 : class
        {
            return hostBuilder
                .ConfigureSingleOutputService<TInput, TOutput>()
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<ISingleOutputService<TInput, TOutput>>(
                        provider =>
                            new AnonymousSingleOutputService<TInput, TOutput>(cloudEvent =>
                                handler(cloudEvent, provider.GetRequiredService<TDep1>(), provider.GetRequiredService<TDep2>(), provider.GetRequiredService<TDep3>())
                            )
                    );
                });
        }

        public static IMotorHostBuilder ConfigureAnonymousSingleOutputService<TInput, TOutput>(
            this IMotorHostBuilder hostBuilder,
            Func<MotorCloudEvent<TInput>, CancellationToken, Task<MotorCloudEvent<TOutput>>> handler)
            where TInput : class
            where TOutput : class
        {
            return hostBuilder
                .ConfigureSingleOutputService<TInput, TOutput>()
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<ISingleOutputService<TInput, TOutput>>(_ =>
                        new AnonymousSingleOutputService<TInput, TOutput>(handler));
                });
        }

        public static IMotorHostBuilder ConfigureAnonymousSingleOutputService<TInput, TOutput, TDep1>(
            this IMotorHostBuilder hostBuilder,
            Func<MotorCloudEvent<TInput>, TDep1, CancellationToken, Task<MotorCloudEvent<TOutput>>> handler)
            where TInput : class
            where TOutput : class
            where TDep1 : class
        {
            return hostBuilder
                .ConfigureSingleOutputService<TInput, TOutput>()
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<ISingleOutputService<TInput, TOutput>>(provider =>
                        new AnonymousSingleOutputService<TInput, TOutput>((cloudEvent, token) =>
                            handler(cloudEvent, provider.GetRequiredService<TDep1>(), token)));
                });
        }

        public static IMotorHostBuilder ConfigureAnonymousSingleOutputService<TInput, TOutput, TDep1, TDep2>(
            this IMotorHostBuilder hostBuilder,
            Func<MotorCloudEvent<TInput>, TDep1, TDep2, CancellationToken, Task<MotorCloudEvent<TOutput>>> handler)
            where TInput : class
            where TOutput : class
            where TDep1 : class
            where TDep2 : class
        {
            return hostBuilder
                .ConfigureSingleOutputService<TInput, TOutput>()
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<ISingleOutputService<TInput, TOutput>>(provider =>
                        new AnonymousSingleOutputService<TInput, TOutput>((cloudEvent, token) =>
                            handler(cloudEvent, provider.GetRequiredService<TDep1>(), provider.GetRequiredService<TDep2>(), token)));
                });
        }

        public static IMotorHostBuilder ConfigureAnonymousSingleOutputService<TInput, TOutput, TDep1, TDep2, TDep3>(
            this IMotorHostBuilder hostBuilder,
            Func<MotorCloudEvent<TInput>, TDep1, TDep2, TDep3, CancellationToken, Task<MotorCloudEvent<TOutput>>>
                handler) where TInput : class
            where TOutput : class
            where TDep1 : class
            where TDep2 : class
            where TDep3 : class
        {
            return hostBuilder
                .ConfigureSingleOutputService<TInput, TOutput>()
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<ISingleOutputService<TInput, TOutput>>(provider =>
                        new AnonymousSingleOutputService<TInput, TOutput>((cloudEvent, token) =>
                            handler(cloudEvent, provider.GetRequiredService<TDep1>(), provider.GetRequiredService<TDep2>(),
                                provider.GetRequiredService<TDep3>(), token)));
                });
        }
    }
}
