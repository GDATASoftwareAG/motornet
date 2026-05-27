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
    public static void AddPgMqWithConfig<T>(this IConsumerBuilder<T> builder, IConfiguration config)
        where T : notnull
    {
        var options =
            config.Get<PgMqConsumerOptions>()
            ?? throw new InvalidOperationException("PgMqConsumerOptions configuration section is missing or invalid");
        config.Bind(options);

        builder.AddConsumer(sp => new PgMqMessageConsumer<T>(
            options,
            sp.GetRequiredService<ILogger<PgMqMessageConsumer<T>>>(),
            sp.GetRequiredService<IHostApplicationLifetime>(),
            sp.GetRequiredService<IApplicationNameService>(),
            new NpgmqClient(options.ToConnectionString())
        ));
    }

    public static void AddPgMq<T>(this IConsumerBuilder<T> builder, string configSection = "PgMqConsumer")
        where T : notnull
    {
        builder.AddPgMqWithConfig(builder.Context.Configuration.GetSection(configSection));
    }

    public static void AddPgMqWithConfig<T>(this IPublisherBuilder<T> builder, IConfiguration config)
        where T : notnull
    {
        builder.AddTransient<CloudEventFormatter, JsonEventFormatter>();

        var options =
            config.Get<PgMqPublisherOptions>()
            ?? throw new InvalidOperationException("PgMqPublisherOptions configuration section is missing or invalid");
        config.Bind(options);

        builder.Configure<PgMqPublisherOptions>(config);
        builder.AddPublisher(sp => new PgMqMessageProducer<T>(
            MSOptions.Create(options),
            sp.GetRequiredService<ILogger<PgMqMessageProducer<T>>>(),
            sp.GetRequiredService<IOptions<PublisherOptions>>(),
            new NpgmqClient(options.ToConnectionString())
        ));
    }

    public static void AddPgMq<T>(this IPublisherBuilder<T> builder, string configSection = "PgMqPublisher")
        where T : notnull
    {
        builder.AddPgMqWithConfig(builder.Context.Configuration.GetSection(configSection));
    }
}
