using Microsoft.Extensions.DependencyInjection;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Internal;

namespace Motor.Extensions.Hosting
{
    public static class QueuedGenericServiceExtensions
    {
        public static IServiceCollection AddQueuedGenericService<TInput>(this IServiceCollection services)
            where TInput : class
        {
            services.AddHostedService<QueuedGenericService<TInput>>();
            services
                .AddSingleton<IBackgroundTaskQueue<MotorCloudEvent<TInput>>,
                    BackgroundTaskQueue<MotorCloudEvent<TInput>>>();
            services.AddTransient<BaseDelegatingMessageHandler<TInput>>();
            services.AddTransient<PrepareDelegatingMessageHandler<TInput>>();
            return services;
        }
    }
}
