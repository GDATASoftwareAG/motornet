using Metrics;
using Metrics.DifferentNamespace;
using Metrics.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Conversion.SystemJson;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Utilities;

await MotorHost.CreateDefaultBuilder()
    // Configure the types of the input and output messages
    .ConfigureNoOutputService<InputMessage>()
    .ConfigureServices((hostContext, services) =>
    {
        // Add a handler for the input message which returns an output message
        // This handler is called for every new incoming message
        services.AddTransient<INoOutputService<InputMessage>, ServiceWithMetrics>();
        services.AddTransient<IServiceInDifferentNamespace, ServiceInDifferentNamespace>();
    })
    // Add the incomming communication module. 
    .ConfigureConsumer<InputMessage>((context, builder) =>
    {
        // In this case the messages are received from RabbitMQ
        builder.AddRabbitMQ();
        // The encoding of the incoming message, such that the handler is able to deserialize the message
        builder.AddSystemJson();
    })
    .RunConsoleAsync();
