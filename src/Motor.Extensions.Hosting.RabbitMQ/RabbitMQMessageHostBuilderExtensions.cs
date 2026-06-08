using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Motor.Extensions.Diagnostics.Queue.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using MSOptions = Microsoft.Extensions.Options.Options;

namespace Motor.Extensions.Hosting.RabbitMQ;

public static class RabbitMQMessageHostBuilderExtensions
{
    extension<T>(IConsumerBuilder<T> builder)
        where T : notnull
    {
        public IConsumerBuilder<T> AddRabbitMQWithConfig(IConfiguration config)
        {
            builder.AddTransient<CloudEventFormatter, JsonEventFormatter>();
            var consumerOptions = new RabbitMQConsumerOptions<T>();
            config.Bind(consumerOptions);
            var connectionFactory = RabbitMQConnectionFactory<T>.From(consumerOptions);
            builder.AddConsumer(sp => new RabbitMQMessageConsumer<T>(
                sp.GetRequiredService<ILogger<RabbitMQMessageConsumer<T>>>(),
                connectionFactory,
                MSOptions.Create(consumerOptions),
                sp.GetRequiredService<IHostApplicationLifetime>(),
                sp.GetRequiredService<IApplicationNameService>()
            ));
            builder.AddSingleton<IQueueMonitor>(sp => new RabbitMQQueueMonitor<T>(
                sp.GetRequiredService<ILogger<RabbitMQQueueMonitor<T>>>(),
                MSOptions.Create(consumerOptions),
                connectionFactory
            ));

            return builder;
        }

        public IConsumerBuilder<T> AddRabbitMQ(string configSection = "RabbitMQConsumer") =>
            builder.AddRabbitMQWithConfig(builder.Configuration.GetSection(configSection));
    }

    extension<T>(IPublisherBuilder<T> builder)
        where T : notnull
    {
        public IPublisherBuilder<T> AddRabbitMQWithConfig(IConfiguration config)
        {
            builder.AddTransient<CloudEventFormatter, JsonEventFormatter>();
            builder.Configure<RabbitMQPublisherOptions<T>>(config);

            var rabbitMqPublisherOptions = config.Get<RabbitMQPublisherOptions<T>>();
            var connectionFactory = RabbitMQConnectionFactory<T>.From(rabbitMqPublisherOptions);
            builder.AddPublisher(sp => new RabbitMQMessagePublisher<T>(
                connectionFactory,
                MSOptions.Create(rabbitMqPublisherOptions),
                sp.GetRequiredService<IOptions<PublisherOptions>>(),
                sp.GetRequiredService<CloudEventFormatter>()
            ));

            return builder;
        }

        public IPublisherBuilder<T> AddRabbitMQ(string configSection = "RabbitMQPublisher") =>
            builder.AddRabbitMQWithConfig(builder.Configuration.GetSection(configSection));
    }
}
