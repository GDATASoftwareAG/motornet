using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Conversion.JsonNet;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Utilities;

await MotorHost.CreateDefaultBuilder()
    .ConfigureNoOutputService<SomeMessage>()
    .ConfigureServices((_, services) =>
    {
        services.AddTransient<INoOutputService<SomeMessage>, SomeNoOutputService>();
    })
    .ConfigureConsumer<SomeMessage>((_, builder) =>
    {
        builder.AddRabbitMQ();
        builder.AddJsonNet();
    })
    .RunConsoleAsync();

public class SomeNoOutputService : INoOutputService<SomeMessage>
{
    public Task<ProcessedMessageStatus> HandleMessageAsync(MotorCloudEvent<SomeMessage> dataCloudEvent,
        CancellationToken token = default)
    {
        var veryComputeIntensiveMessageProcessingResult = dataCloudEvent.TypedData.FancyNumber;
        return veryComputeIntensiveMessageProcessingResult switch
        {
            0 => Task.FromResult(ProcessedMessageStatus.Failure),
            1 => throw new FailureException("i failed"),
            _ => Task.FromResult(ProcessedMessageStatus.Success)
        };
    }
}

public record SomeMessage
{
    public int FancyNumber { get; set; } = 42;
}
