using System;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Motor.Extensions.Conversion.SystemJson;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.Hosting.Nano;
using Motor.Extensions.Hosting.Publisher;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Utilities;

await MotorHost.CreateDefaultBuilder()
    .ConfigureAnonymousSingleOutputService<InputMessage, OutputMessage, ILogger>((cloudEvent, logger) =>
    {
        logger.LogInformation("Handling message - synchronously");
        var data = cloudEvent.TypedData;
        return data switch
        {
            { FancyText: { Length: > 0 } } => cloudEvent.CreateNew(new OutputMessage(data.FancyText.Reverse().ToString(),
                data.FancyNumber * -1)),
            _ => throw new ArgumentNullException("FancyText is empty")
        };
    })
    .ConfigureConsumer<InputMessage>((_, builder) =>
    {
        builder.AddRabbitMQ();
        builder.AddSystemJson();
    })
    .ConfigurePublisher<OutputMessage>((_, builder) =>
    {
        builder.AddRabbitMQ();
        builder.AddSystemJson();
    })
    .RunConsoleAsync();

public record OutputMessage(string NotSoFancyText, int NotSoFancyNumber);

public record InputMessage(string FancyText = "FooBar", int FancyNumber = 42);
