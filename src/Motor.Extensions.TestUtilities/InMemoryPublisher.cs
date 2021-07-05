using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.TestUtilities
{
    public class InMemoryPublisher<T> : ITypedMessagePublisher<byte[]>
    {
        private readonly ConcurrentQueue<MotorCloudEvent<byte[]>> _events = new();

        public IReadOnlyCollection<MotorCloudEvent<byte[]>> Events => _events;

        public Task PublishMessageAsync(MotorCloudEvent<byte[]> cloudEvent, CancellationToken token = default)
        {
            _events.Enqueue(cloudEvent);
            return Task.CompletedTask;
        }
    }
}
