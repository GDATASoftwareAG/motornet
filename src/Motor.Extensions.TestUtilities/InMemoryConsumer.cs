using System;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.TestUtilities;

public class InMemoryConsumer<T> : IMessageConsumer<T>
    where T : notnull
{
    public Func<
        MotorCloudEvent<byte[]>,
        CancellationToken,
        Task<ProcessedMessageStatus>
    >? ConsumeCallbackAsync { get; set; }

    public Task<ProcessedMessageStatus> AddMessage(
        MotorCloudEvent<byte[]> cloudEvent,
        CancellationToken cancellationToken = default
    ) => ConsumeCallbackAsync!.Invoke(cloudEvent, cancellationToken);

    public Task StartAsync(CancellationToken token = default) => Task.CompletedTask;

    public Task StopAsync(CancellationToken token = default) => Task.CompletedTask;
}
