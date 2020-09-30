using System.Threading;
using System.Threading.Tasks;
using Metrics.DifferentNamespace;
using Metrics.Model;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Prometheus.Client;
using Prometheus.Client.Abstractions;

namespace Metrics
{
    public class ServiceWithMetrics : INoOutputService<InputMessage>
    {
        private readonly IServiceInDifferentNamespace _serviceInDifferentNamespace;
        private readonly IMetricFamily<ICounter>? _counter;
        private readonly IMetricFamily<ISummary>? _summary;

        public ServiceWithMetrics(IMetricsFactory<ServiceWithMetrics> metricFactory,
            IServiceInDifferentNamespace serviceInDifferentNamespace)
        {
            _serviceInDifferentNamespace = serviceInDifferentNamespace;
            
            // Resulting label in Prometheus: metrics_emtpy_string_total
            _counter = metricFactory?.CreateCounter("empty_string_total",
                "Counts the total number of recieved empty strings.");
            
            // Resulting label in Prometheus: metrics_fancy_number
            _summary = metricFactory?.CreateSummary("fancy_number", 
                "Shows the distribution of fancy numbers.");
        }

        public Task<ProcessedMessageStatus> HandleMessageAsync(MotorCloudEvent<InputMessage> inputEvent,
            CancellationToken token = new CancellationToken())
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
