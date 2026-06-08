using System;
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

var useMotorHost = bool.Parse(Environment.GetEnvironmentVariable("USE_MOTOR_HOST") ?? "false");

if (useMotorHost)
{
    await MotorHost
        .CreateDefaultBuilder()
        // Configure the types of the input and output messages
        .ConfigureSingleOutputService<IncomingMessage, OutgoingMessage>()
        .ConfigureServices(
            (_, services) =>
            {
                // This handler is called for every new incoming message
                services.AddTransient<ISingleOutputService<IncomingMessage, OutgoingMessage>, SingleOutputService>();
            }
        )
        // Add the incoming communication module.
        .ConfigureConsumer<IncomingMessage>(
            (_, builder) =>
            {
                // In this case the messages are received from NATS
                builder.AddNATS();
                // The encoding of the incoming message, such that the handler is able to deserialize the message
                builder.AddSystemJson();
            }
        )
        .ConfigurePublisher<OutgoingMessage>(
            (_, builder) =>
            {
                // In this case the messages are send to NATS
                builder.AddNATS();
                // The encoding of the outgoing message, such that the handler is able to serialize the message
                builder.AddSystemJson();
            }
        )
        .RunConsoleAsync();
}
else
{
    var builder = Host.CreateApplicationBuilder(args);

    // Configure opinionated defaults for Motor applications, such as logging.
    builder.AddMotorDefaults();

    // Configure the types of the input and output messages
    builder.ConfigureSingleOutputService<IncomingMessage, OutgoingMessage>();

    // This handler is called for every new incoming message
    builder.Services.AddTransient<ISingleOutputService<IncomingMessage, OutgoingMessage>, SingleOutputService>();

    // Add the incoming communication module.
    builder
        .AddConsumer<IncomingMessage>()
        // In this case the messages are received from NATS
        .AddNATS()
        // The encoding of the incoming message, such that the handler is able to deserialize the message
        .AddSystemJson();

    // Add the outgoing communication module.
    builder
        .AddPublisher<OutgoingMessage>()
        // In this case the messages are send to NATS
        .AddNATS()
        // The encoding of the outgoing message, such that the handler is able to serialize the message
        .AddSystemJson();

    await builder.Build().RunAsync();
}
