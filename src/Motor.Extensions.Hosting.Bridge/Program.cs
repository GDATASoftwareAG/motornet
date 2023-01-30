using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Bridge;
using Motor.Extensions.Hosting.Bridge.Internals;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.Hosting.Kafka;
using Motor.Extensions.Hosting.Publisher;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Hosting.SQS;
using Motor.Extensions.Utilities;


await MotorHost.CreateDefaultBuilder()
    .ConfigureSingleOutputService<ByteData, ByteData>()
    .ConfigureServices((_, collection) =>
    {
        collection.AddTransient<ISingleOutputService<ByteData, ByteData>, PassThoughService>();
    })
    .ConfigurePublisher<ByteData>((ctx, builder) =>
    {
        var s = ctx.Configuration["PublisherType"] ?? string.Empty;
        var publisherType = Enum.Parse<BridgePublisherType>(s, true);
        switch (publisherType)
        {
            case BridgePublisherType.RabbitMQ:
                builder.AddRabbitMQ("Publisher");
                break;
            case BridgePublisherType.Kafka:
                builder.AddKafka("Publisher");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        builder.AddSerializer<ByteDataConversions>();
    })
    .ConfigureConsumer<ByteData>((ctx, builder) =>
    {
        var s = ctx.Configuration["ConsumerType"] ?? string.Empty;
        var consumerType = Enum.Parse<BridgeConsumerType>(s, true);
        switch (consumerType)
        {
            case BridgeConsumerType.RabbitMQ:
                builder.AddRabbitMQ("Consumer");
                break;
            case BridgeConsumerType.Kafka:
                builder.AddKafka("Consumer");
                break;
            case BridgeConsumerType.SQS:
                builder.AddSQS("Consumer");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        builder.AddDeserializer<ByteDataConversions>();
    })
    .RunConsoleAsync();
