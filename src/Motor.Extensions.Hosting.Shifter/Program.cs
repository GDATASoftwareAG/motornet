using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.Hosting.Kafka;
using Motor.Extensions.Hosting.Publisher;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Hosting.Shifter;
using Motor.Extensions.Hosting.Shifter.Internals;
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
        var s = ctx.Configuration["PublisherType"];
        var shifterPublisherType = Enum.Parse<ShifterPublisherType>(s, true);
        switch (shifterPublisherType)
        {
            case ShifterPublisherType.RabbitMQ:
                builder.AddRabbitMQ("Publisher");
                break;
            case ShifterPublisherType.Kafka:
                builder.AddKafka("Publisher");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        builder.AddSerializer<ByteDataConversions>();
    })
    .ConfigureConsumer<ByteData>((ctx, builder) =>
    {
        var s = ctx.Configuration["ConsumerType"];
        var shifterConsumerType = Enum.Parse<ShifterConsumerType>(s, true);
        switch (shifterConsumerType)
        {
            case ShifterConsumerType.RabbitMQ:
                builder.AddRabbitMQ("Consumer");
                break;
            case ShifterConsumerType.Kafka:
                builder.AddKafka("Consumer");
                break;
            case ShifterConsumerType.SQS:
                builder.AddSQS("Consumer");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        builder.AddDeserializer<ByteDataConversions>();
    })
    .RunConsoleAsync();
