using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Prometheus.Client;

namespace MetricsExample.DifferentNamespace
{
    public interface IServiceInDifferentNamespace
    {
        void CountInDifferentNamespace();
    }

    public class ServiceInDifferentNamespace : IServiceInDifferentNamespace
    {
        private readonly ICounter? _counter;

        public ServiceInDifferentNamespace(IMetricsFactory<ServiceInDifferentNamespace>? metricsFactory)
        {
            // Resulting label in Prometheus: metricsexample_differentnamespace_counter_in_different_namespace
            _counter = metricsFactory?.CreateCounter("counter_in_different_namespace", "This counts something else.");
        }

        public void CountInDifferentNamespace()
        {
            _counter?.Inc();
        }
    }
}
