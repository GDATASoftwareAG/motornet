using ConsumeAndPublishWithPgMq.Model;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;

namespace ConsumeAndPublishWithPgMq;

public class SingleOutputService : ISingleOutputService<InputMessage, OutputMessage>
{
    public Task<MotorCloudEvent<OutputMessage>?> ConvertMessageAsync(
        MotorCloudEvent<InputMessage> dataCloudEvent,
        CancellationToken token = default
    )
    {
        var input = dataCloudEvent.TypedData;
        var output = DoSomething(input);
        Console.WriteLine(output.NotSoFancyText);
        var outputEvent = dataCloudEvent.CreateNew(output);
        return Task.FromResult(outputEvent)!;
    }

    private static OutputMessage DoSomething(InputMessage incomingMessage)
    {
        return new OutputMessage { NotSoFancyText = new string(incomingMessage.FancyText.Reverse().ToArray()) };
    }
}
