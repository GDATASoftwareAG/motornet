using System;
using Prometheus.Client;

namespace Motor.Extensions.Diagnostics.Metrics;

public class AutoObserve : IDisposable
{
    private readonly Func<IValueObserver?> _observerDelegate;
    private readonly Func<double> _valueDelegate;

    public AutoObserve(Func<IValueObserver?> observerDelegate, Func<double> valueDelegate)
    {
        _observerDelegate = observerDelegate;
        _valueDelegate = valueDelegate;
    }

    public void Dispose()
    {
        _observerDelegate.Invoke()?.Observe(_valueDelegate.Invoke());
        GC.SuppressFinalize(this);
    }
}
