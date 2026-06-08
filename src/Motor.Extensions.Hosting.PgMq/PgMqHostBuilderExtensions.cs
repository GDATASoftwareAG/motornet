using System;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.PgMq.Options;
using Npgmq;
using MSOptions = Microsoft.Extensions.Options.Options;

namespace Motor.Extensions.Hosting.PgMq;

public static class PgMqHostBuilderExtensions
{
    extension<T>(IConsumerBuilder<T> builder)
        where T : notnull
    {
        public IConsumerBuilder<T> AddPgMqWithConfig(IConfiguration config)
        {
            var options =
                config.Get<PgMqConsumerOptions>()
                ?? throw new InvalidOperationException(
                    "PgMqConsumerOptions configuration section is missing or invalid"
                );
            config.Bind(options);

            builder.AddConsumer(sp => new PgMqMessageConsumer<T>(
                options,
                sp.GetRequiredService<ILogger<PgMqMessageConsumer<T>>>(),
                sp.GetRequiredService<IHostApplicationLifetime>(),
                sp.GetRequiredService<IApplicationNameService>(),
                new NpgmqClient(options.ToConnectionString())
            ));

            return builder;
        }

        public IConsumerBuilder<T> AddPgMq(string configSection = "PgMqConsumer") =>
            builder.AddPgMqWithConfig(builder.Configuration.GetSection(configSection));
    }

    extension<T>(IPublisherBuilder<T> builder)
        where T : notnull
    {
        public IPublisherBuilder<T> AddPgMqWithConfig(IConfiguration config)
        {
            builder.AddTransient<CloudEventFormatter, JsonEventFormatter>();

            var options =
                config.Get<PgMqPublisherOptions>()
                ?? throw new InvalidOperationException(
                    "PgMqPublisherOptions configuration section is missing or invalid"
                );
            config.Bind(options);

            builder.Configure<PgMqPublisherOptions>(config);
            builder.AddPublisher(sp => new PgMqMessageProducer<T>(
                MSOptions.Create(options),
                sp.GetRequiredService<ILogger<PgMqMessageProducer<T>>>(),
                sp.GetRequiredService<IOptions<PublisherOptions>>(),
                new NpgmqClient(options.ToConnectionString())
            ));

            return builder;
        }

        public IPublisherBuilder<T> AddPgMq(string configSection = "PgMqPublisher") =>
            builder.AddPgMqWithConfig(builder.Configuration.GetSection(configSection));
    }
}
