using ConsumeAndPublishWithRabbitMQ;
using ConsumeAndPublishWithRabbitMQ.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Compression.Gzip;
using Motor.Extensions.Conversion.SystemJson;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.Hosting.Publisher;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Utilities;

await MotorHost.CreateDefaultBuilder()
    // Configure the types of the input and output messages
    .ConfigureSingleOutputService<InputMessage, OutputMessage>()
    .ConfigureServices((_, services) =>
    {
        // Add a handler for the input message which returns an output message
        // This handler is called for every new incoming message
        services.AddTransient<ISingleOutputService<InputMessage, OutputMessage>, SingleOutputService>();
    })
    // Add the incomming communication module. 
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
    // Add the outgoing communication module.
    .ConfigurePublisher<OutputMessage>((_, builder) =>
    {
        // In this case the messages are send to RabbitMQ
        builder.AddRabbitMQ();
        // The encoding of the outgoing message, such that the handler is able to serialize the message
        builder.AddSystemJson();
        // (Optional) Compress the serialized data of the outgoing message with gzip.
        builder.AddGzipCompression();
    })
    .RunConsoleAsync();
