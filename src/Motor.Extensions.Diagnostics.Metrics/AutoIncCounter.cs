using System;
using Prometheus.Client;

namespace Motor.Extensions.Diagnostics.Metrics;

public class AutoIncCounter : IDisposable
{
    private readonly Func<ICounter?> _counterDelegate;

    public AutoIncCounter(Func<ICounter?> counterDelegate)
    {
        _counterDelegate = counterDelegate;
    }

    public void Dispose()
    {
        _counterDelegate.Invoke()?.Inc();
        GC.SuppressFinalize(this);
    }
}
