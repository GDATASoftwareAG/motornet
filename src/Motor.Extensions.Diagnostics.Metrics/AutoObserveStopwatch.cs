using System;
using System.Diagnostics;
using Prometheus.Client;

namespace Motor.Extensions.Diagnostics.Metrics;

public class AutoObserveStopwatch : IDisposable
{
    private readonly Stopwatch _watch;
    private readonly Func<IValueObserver?> _observerDelegate;

    public AutoObserveStopwatch(Func<IValueObserver?> observerDelegate)
    {
        _observerDelegate = observerDelegate;
        _watch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        _watch.Stop();
        _observerDelegate.Invoke()?.Observe(_watch.ElapsedMilliseconds);
        GC.SuppressFinalize(this);
    }
}
