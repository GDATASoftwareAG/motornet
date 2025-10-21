using System;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.Hosting.Abstractions;

public interface IMessageConsumer<TInput>
    where TInput : notnull
{
    Func<MotorCloudEvent<byte[]>, CancellationToken, Task<ProcessedMessageStatus>>? ConsumeCallbackAsync { get; set; }

    Task StartAsync(CancellationToken token = default);

    async Task ExecuteAsync(CancellationToken token = default)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(100), token).ConfigureAwait(false);
        }
    }

    Task StopAsync(CancellationToken token = default);
}
