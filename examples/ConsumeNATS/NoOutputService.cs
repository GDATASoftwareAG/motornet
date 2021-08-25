using System.Threading;
using System.Threading.Tasks;
using ConsumeNATS.Model;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;

namespace ConsumeNATS
{
    public class NoOutputService : INoOutputService<NatsMessage>
    {
        public Task<ProcessedMessageStatus> HandleMessageAsync(MotorCloudEvent<NatsMessage> dataCloudEvent, CancellationToken token = new())
        {
            System.Console.WriteLine(dataCloudEvent.TypedData.SomeProperty);
            return Task.FromResult(ProcessedMessageStatus.Success);
        }
    }
}
