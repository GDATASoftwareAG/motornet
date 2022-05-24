using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Conversion.SystemJson;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Utilities;

await MotorHost.CreateDefaultBuilder()
    // Configure input message type
    .ConfigureNoOutputService<SomeMessage>()
    .ConfigureServices((_, services) =>
    {
        // Add a handler for the input message
        // This handler is called for every new incoming message
        services.AddTransient<INoOutputService<SomeMessage>, SomeNoOutputService>();
    })
    // Add the incoming communication module. 
    .ConfigureConsumer<SomeMessage>((_, builder) =>
    {
        // In this case the messages are received from RabbitMQ
        builder.AddRabbitMQ();
        // The encoding of the incoming message, such that the handler is able to deserialize the message
        builder.AddSystemJson();
    })
    .RunConsoleAsync();


// This is the message handler, we added above
public class SomeNoOutputService : INoOutputService<SomeMessage>
{
    // Handle incoming messages
    public Task<ProcessedMessageStatus> HandleMessageAsync(MotorCloudEvent<SomeMessage> inputEvent,
        CancellationToken token = default)
    {
        // Get the input message from the cloud event
        var input = inputEvent.TypedData;
        if (!IsValid(input))
        {
            // Depending on the configuration of RepublishInvalidInputToDeadLetterExchange (cf. appsettings.json)
            // the message will be either republished to the configured dead Letter exchange if set to true

            // However, if set to false, which is also the default, the message will be acknowledged
            return Task.FromResult(ProcessedMessageStatus.InvalidInput);
        }

        // Process the message
        var processingResult = ProcessMessage(input);

        // Depending on our processingResult
        return processingResult switch
        {
            // We can either return ProcessedMessageStatus.Failure or throw a
            // FailureException to indicate that message processing was not successful
            // and republish the message to the configured dead letter exchange
            0 => Task.FromResult(ProcessedMessageStatus.Failure),
            1 => throw new FailureException("i failed"),

            // Return ProcessedMessageStatus.Success when message processing succeeds
            _ => Task.FromResult(ProcessedMessageStatus.Success)
        };
    }

    private static int ProcessMessage(SomeMessage message)
    {
        return message.FancyNumber * message.FancyNumber;
    }

    private static bool IsValid(SomeMessage message)
    {
        return message.FancyNumber > 0;
    }
}

public record SomeMessage
{
    public int FancyNumber { get; set; } = 42;
}
