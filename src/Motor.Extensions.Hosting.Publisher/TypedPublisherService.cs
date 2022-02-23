using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Hosting.Publisher;

public class TypedPublisherService<TOutput> : IHostedService where TOutput : class
{
    private readonly List<ITypedMessagePublisher<TOutput>> _publishers;

    public TypedPublisherService(IEnumerable<ITypedMessagePublisher<TOutput>> publishers)
    {
        _publishers = publishers.ToList();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(_publishers.Select(p => p.StartAsync(cancellationToken)));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(_publishers.Select(p => p.StopAsync(cancellationToken)));
    }
}
