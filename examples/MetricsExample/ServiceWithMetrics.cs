using System.Threading;
using System.Threading.Tasks;
using MetricsExample.DifferentNamespace;
using MetricsExample.Model;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Prometheus.Client.Abstractions;

namespace MetricsExample
{
    public class ServiceWithMetrics : INoOutputService<InputMessage>
    {
        private readonly IServiceInDifferentNamespace _serviceInDifferentNamespace;
        private readonly ICounter? _counter;
        private readonly ISummary? _summary;

        public ServiceWithMetrics(IMetricsFactory<ServiceWithMetrics> metricFactory,
            IServiceInDifferentNamespace serviceInDifferentNamespace)
        {
            _serviceInDifferentNamespace = serviceInDifferentNamespace;

            // Resulting label in Prometheus: metricsexample_emtpy_string_total
            _counter = metricFactory?.CreateCounter("empty_string_total",
                "Counts the total number of recieved empty strings.");

            // Resulting label in Prometheus: metricsexample_fancy_number
            _summary = metricFactory?.CreateSummary("fancy_number",
                "Shows the distribution of fancy numbers.");
        }

        public Task<ProcessedMessageStatus> HandleMessageAsync(MotorCloudEvent<InputMessage> inputEvent,
            CancellationToken token = default)
        {
            // Handle incoming messages
            // Get the input message from the cloud event
            var input = inputEvent.TypedData;

            // Do your magic here .....
            MagicFunc(input);

            return Task.FromResult(ProcessedMessageStatus.Success);
        }

        // Use metrics
        private void MagicFunc(InputMessage input)
        {
            if (input.FancyText == "")
            {
                _counter?.Inc();
            }

            _summary?.Observe(input.FancyNumber);

            _serviceInDifferentNamespace.CountInDifferentNamespace();
        }
    }
}
