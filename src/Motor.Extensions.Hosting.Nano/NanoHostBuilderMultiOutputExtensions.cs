using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Utilities;
using Motor.Extensions.Utilities.Abstractions;

namespace Motor.Extensions.Hosting.Nano
{
    public static class NanoHostBuilderExtensions
    {
        public static IMotorHostBuilder ConfigureAnonymousMultiOutputService<TInput, TOutput>(
            this IMotorHostBuilder hostBuilder,
            Func<MotorCloudEvent<TInput>, IEnumerable<MotorCloudEvent<TOutput>>> handler)
            where TInput : class
            where TOutput : class
        {
            return hostBuilder
                .ConfigureMultiOutputService<TInput, TOutput>()
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<IMultiOutputService<TInput, TOutput>>(_ =>
                        new AnonymousMultiOutputService<TInput, TOutput>(handler));
                });
        }

        public static IMotorHostBuilder ConfigureAnonymousMultiOutputService<TInput, TOutput, TDep1>(
            this IMotorHostBuilder hostBuilder,
            Func<MotorCloudEvent<TInput>, TDep1, IEnumerable<MotorCloudEvent<TOutput>>>
                handler)
            where TInput : class
            where TOutput : class
            where TDep1 : class
        {
            return hostBuilder
                .ConfigureMultiOutputService<TInput, TOutput>()
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<IMultiOutputService<TInput, TOutput>>(provider =>
                        new AnonymousMultiOutputService<TInput, TOutput>((cloudEvent) =>
                            handler(cloudEvent, provider.GetRequiredService<TDep1>()))
                    );
                });
        }

        public static IMotorHostBuilder ConfigureAnonymousMultiOutputService<TInput, TOutput, TDep1, TDep2>(
            this IMotorHostBuilder hostBuilder,
            Func<MotorCloudEvent<TInput>, TDep1, TDep2, IEnumerable<MotorCloudEvent<TOutput>>>
                handler)
            where TInput : class
            where TOutput : class
            where TDep1 : class
            where TDep2 : class
        {
            return hostBuilder
                .ConfigureMultiOutputService<TInput, TOutput>()
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<IMultiOutputService<TInput, TOutput>>(provider =>
                        new AnonymousMultiOutputService<TInput, TOutput>((cloudEvent) =>
                            handler(cloudEvent, provider.GetRequiredService<TDep1>(), provider.GetRequiredService<TDep2>()))
                    );
                });
        }

        public static IMotorHostBuilder ConfigureAnonymousMultiOutputService<TInput, TOutput, TDep1, TDep2, TDep3>(
            this IMotorHostBuilder hostBuilder,
            Func<MotorCloudEvent<TInput>, TDep1, TDep2, TDep3,
                    IEnumerable<MotorCloudEvent<TOutput>>>
                handler)
            where TInput : class
            where TOutput : class
            where TDep1 : class
            where TDep2 : class
            where TDep3 : class
        {
            return hostBuilder
                .ConfigureMultiOutputService<TInput, TOutput>()
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<IMultiOutputService<TInput, TOutput>>(provider =>
                        new AnonymousMultiOutputService<TInput, TOutput>((cloudEvent) =>
                            handler(cloudEvent, provider.GetRequiredService<TDep1>(), provider.GetRequiredService<TDep2>(),
                                provider.GetRequiredService<TDep3>()))
                    );
                });
        }

        public static IMotorHostBuilder ConfigureAnonymousMultiOutputService<TInput, TOutput>(
            this IMotorHostBuilder hostBuilder,
            Func<MotorCloudEvent<TInput>, CancellationToken, Task<IEnumerable<MotorCloudEvent<TOutput>>>> handler)
            where TInput : class
            where TOutput : class
        {
            return hostBuilder
                .ConfigureMultiOutputService<TInput, TOutput>()
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<IMultiOutputService<TInput, TOutput>>(_ =>
                        new AnonymousMultiOutputService<TInput, TOutput>(handler));
                });
        }

        public static IMotorHostBuilder ConfigureAnonymousMultiOutputService<TInput, TOutput, TDep1>(
            this IMotorHostBuilder hostBuilder,
            Func<MotorCloudEvent<TInput>, TDep1, CancellationToken, Task<IEnumerable<MotorCloudEvent<TOutput>>>>
                handler)
            where TInput : class
            where TOutput : class
            where TDep1 : class
        {
            return hostBuilder
                .ConfigureMultiOutputService<TInput, TOutput>()
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<IMultiOutputService<TInput, TOutput>>(provider =>
                        new AnonymousMultiOutputService<TInput, TOutput>((cloudEvent, token) =>
                            handler(cloudEvent, provider.GetRequiredService<TDep1>(), token))
                    );
                });
        }

        public static IMotorHostBuilder ConfigureAnonymousMultiOutputService<TInput, TOutput, TDep1, TDep2>(
            this IMotorHostBuilder hostBuilder,
            Func<MotorCloudEvent<TInput>, TDep1, TDep2, CancellationToken, Task<IEnumerable<MotorCloudEvent<TOutput>>>>
                handler)
            where TInput : class
            where TOutput : class
            where TDep1 : class
            where TDep2 : class
        {
            return hostBuilder
                .ConfigureMultiOutputService<TInput, TOutput>()
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<IMultiOutputService<TInput, TOutput>>(provider =>
                        new AnonymousMultiOutputService<TInput, TOutput>((cloudEvent, token) =>
                            handler(cloudEvent, provider.GetRequiredService<TDep1>(), provider.GetRequiredService<TDep2>(), token))
                    );
                });
        }

        public static IMotorHostBuilder ConfigureAnonymousMultiOutputService<TInput, TOutput, TDep1, TDep2, TDep3>(
            this IMotorHostBuilder hostBuilder,
            Func<MotorCloudEvent<TInput>, TDep1, TDep2, TDep3, CancellationToken,
                    Task<IEnumerable<MotorCloudEvent<TOutput>>>>
                handler)
            where TInput : class
            where TOutput : class
            where TDep1 : class
            where TDep2 : class
            where TDep3 : class
        {
            return hostBuilder
                .ConfigureMultiOutputService<TInput, TOutput>()
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient<IMultiOutputService<TInput, TOutput>>(provider =>
                        new AnonymousMultiOutputService<TInput, TOutput>((cloudEvent, token) =>
                            handler(cloudEvent, provider.GetRequiredService<TDep1>(), provider.GetRequiredService<TDep2>(),
                                provider.GetRequiredService<TDep3>(), token))
                    );
                });
        }
    }
}
