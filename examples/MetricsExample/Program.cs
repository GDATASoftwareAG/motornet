using MetricsExample;
using MetricsExample.Model;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Conversion.SystemJson;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Utilities;

await MotorHost
    .CreateDefaultBuilder()
    // Configure the types of the input and output messages
    .ConfigureNoOutputService<InputMessage>()
    // Add the incomming communication module.
    .ConfigureConsumer<InputMessage>(
        (_, builder) =>
        {
            // In this case the messages are received from RabbitMQ
            builder.AddRabbitMQ();
            // The encoding of the incoming message, such that the handler is able to deserialize the message
            builder.AddSystemJson();
        }
    )
    .UseStartup<Startup>()
    .RunConsoleAsync();
