using PublishToMultipleQueuesRabbitMQ.Model;
using PublishToMultipleQueuesRabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.ContentEncoding.Gzip;
using Motor.Extensions.Conversion.SystemJson;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.Hosting.Publisher;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Utilities;

await MotorHost.CreateDefaultBuilder()
    // Configure the types of the input message
    .ConfigureNoOutputService<InputMessage>()
    .ConfigureServices((_, services) =>
    {
        // Add a handler for the input message. this handler is a INoOutputService
        // because we would have to specify the output type here, which we can't because
        // the handler wants to send to different queues, depending on the incoming
        // message data and its associated business logic.
        // This handler is called for every new incoming message.
        services.AddTransient<INoOutputService<InputMessage>, NoOutputService>();
    })
    // Add the incoming communication modules.
    .ConfigureConsumer<InputMessage>((_, builder) =>
    {
        // In this case the messages are received from RabbitMQ
        builder.AddRabbitMQ();
        // The encoding of the incoming message, such that the handler is able to deserialize the message
        builder.AddSystemJson();
        // (Optional) Enable support for incoming messages that are gzip compressed. Uncompressed messages will still
        //  work to make the migration to compression backwards-compatible.
        builder.AddGzipDecompression();
    })
    // Now add the different queues. For each individual queue
    // we have to add here individual sections. Because of the template argument, each
    // section needs to have its own message type. You can currently not send the same
    // message type to different queues. Note that these dont have to be separate objects though.
    // So if you want to send the same message to multiple queues, you can define your data in a
    // base class, and then create an empty derived type, which is to be used here in the setup (as
    // shown in this example).

    // Add one publishing queue.
    .ConfigurePublisher<LeftMessage>((_, builder) =>
    {
        // In this case the messages are sent to one RabbitMQ queue.
        // We could still use the default name for any of the config sections, but
        // using a descriptive name is better, especially because we still have
        // the other queue, or as many as needed, with different settings.
        // Obviously only one queue can have the default name anyway.
        builder.AddRabbitMQ("LeftQueue");

        // The encoding of the outgoing message, such that the handler is able to serialize the message
        builder.AddSystemJson();

        // (Optional) Compress the serialized data of the outgoing message with gzip.
        builder.AddGzipCompression();
    })
    // Add another publishing queue.
    .ConfigurePublisher<RightMessage>((_, builder) =>
    {
        // In this case the messages are sent to the other RabbitMQ queue.
        builder.AddRabbitMQ("RightQueue");

        // The encoding of the outgoing message, such that the handler is able to serialize the message
        builder.AddSystemJson();

        // (Optional) Compress the serialized data of the outgoing message with gzip.
        builder.AddGzipCompression();
    })
    .RunConsoleAsync();
