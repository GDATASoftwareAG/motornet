using ConsumeNATS.Model;
using ConsumeNATS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Conversion.SystemJson;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.Hosting.NATS;
using Motor.Extensions.Utilities;

await MotorHost.CreateDefaultBuilder()
    // Configure the types of the input messages
    .ConfigureNoOutputService<NatsMessage>()
    .ConfigureServices((_, services) =>
    {
        // This handler is called for every new incoming message
        services.AddTransient<INoOutputService<NatsMessage>, NoOutputService>();
    })
    // Add the incoming communication module. 
    .ConfigureConsumer<NatsMessage>((_, builder) =>
    {
        // In this case the messages are received from AWS SQS
        builder.AddNATS();
        // The encoding of the incoming message, such that the handler is able to deserialize the message
        builder.AddSystemJson();
    })
    .RunConsoleAsync();
