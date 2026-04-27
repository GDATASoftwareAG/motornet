using System;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.Hosting.PgMq;

public sealed class PgMqMessageConsumer<TData> : IMessageConsumer<TData>
    where TData : notnull
{
    public Func<MotorCloudEvent<byte[]>, CancellationToken, Task<ProcessedMessageStatus>>? ConsumeCallbackAsync
    {
        get;
        set;
    }

    public Task StartAsync(CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task StopAsync(CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}
