using System;
using System.Threading;
using System.Threading.Tasks;
using ConsumeAndPublishNATS.Model;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;

namespace ConsumeAndPublishNATS;

public class SingleOutputService : ISingleOutputService<IncomingMessage, OutgoingMessage>
{
    public Task<MotorCloudEvent<OutgoingMessage>> ConvertMessageAsync(
        MotorCloudEvent<IncomingMessage> dataCloudEvent,
        CancellationToken token = default
    )
    {
        Console.WriteLine(dataCloudEvent.TypedData.SomeProperty);
        return Task.FromResult(dataCloudEvent.CreateNew(DoSomething(dataCloudEvent.TypedData)));
    }

    private OutgoingMessage DoSomething(IncomingMessage incomingMessage)
    {
        return new OutgoingMessage
        {
            SomeProperty = incomingMessage.SomeProperty,
            IncomingTime = incomingMessage.IncomingTime,
            OutgoingTime = DateTime.UtcNow,
        };
    }
}
