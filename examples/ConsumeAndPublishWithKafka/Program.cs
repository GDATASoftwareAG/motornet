using System;
using ConsumeAndPublishWithKafka;
using ConsumeAndPublishWithKafka.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Conversion.SystemJson;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.Hosting.Kafka;
using Motor.Extensions.Hosting.Publisher;
using Motor.Extensions.Utilities;

var useMotorHost = bool.Parse(Environment.GetEnvironmentVariable("USE_MOTOR_HOST") ?? "false");

if (useMotorHost)
{
    await MotorHost
        .CreateDefaultBuilder()
        // Configure the types of the input and output messages
        .ConfigureSingleOutputService<InputMessage, OutputMessage>()
        .ConfigureServices(
            (_, services) =>
            {
                // Add a handler for the input message which returns an output message
                // This handler is called for every new incoming message
                services.AddTransient<ISingleOutputService<InputMessage, OutputMessage>, SingleOutputService>();
            }
        )
        // Add the incomming communication module.
        .ConfigureConsumer<InputMessage>(
            (_, builder) =>
            {
                // In this case the messages are received from Kafka
                builder.AddKafka();
                // The encoding of the incoming message, such that the handler is able to deserialize the message
                builder.AddSystemJson();
            }
        )
        // Add the outgoing communication module.
        .ConfigurePublisher<OutputMessage>(
            (_, builder) =>
            {
                // In this case the messages are send to Kafka
                builder.AddKafka();
                // The encoding of the outgoing message, such that the handler is able to serialize the message
                builder.AddSystemJson();
            }
        )
        .RunConsoleAsync();
}
else
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.AddMotorDefaults(assembly: typeof(Program).Assembly);

    // Configure the types of the input and output messages
    builder.ConfigureSingleOutputService<InputMessage, OutputMessage>();

    // Add a handler for the input message which returns an output message
    // This handler is called for every new incoming message
    builder.Services.AddTransient<ISingleOutputService<InputMessage, OutputMessage>, SingleOutputService>();

    // Add the incomming communication module.
    builder
        .AddConsumer<InputMessage>()
        // In this case the messages are received from Kafka
        .AddKafka()
        // The encoding of the incoming message, such that the handler is able to deserialize the message
        .AddSystemJson();

    // Add the outgoing communication module.
    builder
        .AddPublisher<OutputMessage>()
        // In this case the messages are send to Kafka
        .AddKafka()
        // The encoding of the outgoing message, such that the handler is able to serialize the message
        .AddSystemJson();

    await builder.Build().RunAsync();
}

public partial class Program
{
    protected Program() { }
};
