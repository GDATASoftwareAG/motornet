using ConsumeAndPublishNATS;
using ConsumeAndPublishNATS.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Conversion.SystemJson;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.Hosting.NATS;
using Motor.Extensions.Hosting.Publisher;
using Motor.Extensions.Utilities;

await MotorHost.CreateDefaultBuilder()
    // Configure the types of the input messages
    .ConfigureSingleOutputService<IncomingMessage, OutgoingMessage>()
    .ConfigureServices((_, services) =>
    {
        // This handler is called for every new incoming message
        services.AddTransient<ISingleOutputService<IncomingMessage, OutgoingMessage>, SingleOutputService>();
    })
    // Add the incoming communication module. 
    .ConfigureConsumer<IncomingMessage>((_, builder) =>
    {
        // In this case the messages are received from AWS SQS
        builder.AddNATS();
        // The encoding of the incoming message, such that the handler is able to deserialize the message
        builder.AddSystemJson();
    })
    .ConfigurePublisher<OutgoingMessage>((_, builder) =>
    {
        // In this case the messages are send to Kafka
        builder.AddNATS();
        // The encoding of the outgoing message, such that the handler is able to serialize the message
        builder.AddSystemJson();
    })
    .RunConsoleAsync();
