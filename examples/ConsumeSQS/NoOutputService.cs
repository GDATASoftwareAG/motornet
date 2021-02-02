using System;
using System.Threading;
using System.Threading.Tasks;
using ConsumeSQS.Model;
using Motor.Extensions.Hosting.Abstractions;

namespace ConsumeSQS
{
    public class NoOutputService : INoOutputService<InputMessage>
    {

        public Task<ProcessedMessageStatus> HandleMessageAsync(MotorCloudEvent<InputMessage> dataCloudEvent, CancellationToken token = default)
        {
            var msg = dataCloudEvent?.TypedData;
            Console.WriteLine(msg);
            return Task.FromResult(ProcessedMessageStatus.Success);
        }
    }
}
