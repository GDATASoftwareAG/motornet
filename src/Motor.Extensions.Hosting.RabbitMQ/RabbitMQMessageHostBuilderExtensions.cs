using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MSOptions = Microsoft.Extensions.Options.Options;
using Motor.Extensions.Diagnostics.Queue.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.RabbitMQ.Options;

namespace Motor.Extensions.Hosting.RabbitMQ
{
    public static class RabbitMQMessageHostBuilderExtensions
    {
        public static void AddRabbitMQWithConfig<T>(this IConsumerBuilder<T> builder, IConfiguration config) where T : notnull
        {
            builder.AddTransient<CloudEventFormatter, JsonEventFormatter>();
            var consumerOptions = new RabbitMQConsumerOptions<T>();
            config.Bind(consumerOptions);
            var connectionFactory = RabbitMQConnectionFactory<T>.From(consumerOptions);
            builder.AddConsumer(sp => new RabbitMQMessageConsumer<T>(
                sp.GetRequiredService<ILogger<RabbitMQMessageConsumer<T>>>(), connectionFactory,
                MSOptions.Create(consumerOptions),
                sp.GetRequiredService<IHostApplicationLifetime>(),
                sp.GetRequiredService<IApplicationNameService>()));
            builder.AddSingleton<IQueueMonitor>(sp =>
                new RabbitMQQueueMonitor<T>(sp.GetRequiredService<ILogger<RabbitMQQueueMonitor<T>>>(),
                    MSOptions.Create(consumerOptions), connectionFactory));
        }

        public static void AddRabbitMQ<T>(this IConsumerBuilder<T> builder, string configSection = "RabbitMQConsumer") where T : notnull
        {
            builder.AddRabbitMQWithConfig(builder.Context.Configuration.GetSection(configSection));
        }

        public static void AddRabbitMQWithConfig<T>(this IPublisherBuilder<T> builder, IConfiguration config) where T : notnull
        {
            builder.AddTransient<CloudEventFormatter, JsonEventFormatter>();
            builder.Configure<RabbitMQPublisherOptions<T>>(config);
            var publisherOptions = new RabbitMQPublisherOptions<T>();
            config.Bind(publisherOptions);
            var connectionFactory = RabbitMQConnectionFactory<T>.From(publisherOptions);
            builder.AddPublisher(sp
             => new RabbitMQMessagePublisher<T>(connectionFactory,
                 MSOptions.Create(publisherOptions),
                 sp.GetRequiredService<CloudEventFormatter>()));
        }

        public static void AddRabbitMQ<T>(this IPublisherBuilder<T> builder, string configSection = "RabbitMQPublisher") where T : notnull
        {
            builder.AddRabbitMQWithConfig(builder.Context.Configuration.GetSection(configSection));
        }
    }
}
