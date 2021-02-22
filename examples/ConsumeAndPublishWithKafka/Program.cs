using ConsumeAndPublishWithKafka;
using ConsumeAndPublishWithKafka.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Conversion.SystemJson;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.Hosting.Publisher;
using Motor.Extensions.Hosting.Kafka;
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
        // In this case the messages are received from Kafka
        builder.AddKafka();
        // The encoding of the incoming message, such that the handler is able to deserialize the message
        builder.AddSystemJson();
    })
    // Add the outgoing communication module.
    .ConfigurePublisher<OutputMessage>((_, builder) =>
    {
        // In this case the messages are send to Kafka
        builder.AddKafka();
        // The encoding of the outgoing message, such that the handler is able to serialize the message
        builder.AddSystemJson();
    })
    .RunConsoleAsync();
