using System;
using System.Diagnostics;
using System.Linq;
using Prometheus.Client;

namespace Motor.Extensions.Diagnostics.Metrics
{
    public class AutoObserveStopwatch : IDisposable
    {
        private readonly Stopwatch _watch;
        private readonly ISummary? _unlabeledSummary;
        private readonly IMetricFamily<ISummary>? _labeledSummary;
        private readonly AbstractLabel[] _labels = Array.Empty<AbstractLabel>();

        public AutoObserveStopwatch(ISummary? unlabeledSummary)
        {
            _unlabeledSummary = unlabeledSummary;
            _watch = Stopwatch.StartNew();
        }

        public AutoObserveStopwatch(IMetricFamily<ISummary>? labeledSummary, params AbstractLabel[] labels)
        {
            _labeledSummary = labeledSummary;
            _labels = labels;
            _watch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _watch.Stop();
            _labeledSummary?.WithLabels(_labels.Select(l => l.ToString()).ToArray()).Observe(_watch.ElapsedMilliseconds);
            _unlabeledSummary?.Observe(_watch.ElapsedMilliseconds);
            GC.SuppressFinalize(this);
        }
    }
}
