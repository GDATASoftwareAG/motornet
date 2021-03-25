using System;
using System.Diagnostics;
using System.Linq;
using Prometheus.Client;

namespace Motor.Extensions.Diagnostics.Metrics
{
    public class AutoIncCounter : IDisposable
    {
        private readonly ICounter? _unlabeledCounter;
        private readonly IMetricFamily<ICounter>? _labeledCounter;
        private readonly AbstractLabel[] _labels = Array.Empty<AbstractLabel>();

        public AutoIncCounter(ICounter? unlabeledCounter)
        {
            _unlabeledCounter = unlabeledCounter;
        }

        public AutoIncCounter(IMetricFamily<ICounter>? labeledCounter, params AbstractLabel[] labels)
        {
            _labeledCounter = labeledCounter;
            _labels = labels;
        }

        public void Dispose()
        {
            _labeledCounter?.WithLabels(_labels.Select(l => l.ToString()).ToArray()).Inc();
            _unlabeledCounter?.Inc();
            GC.SuppressFinalize(this);
        }
    }
}
