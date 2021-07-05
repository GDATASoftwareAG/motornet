using System.Threading;
using System.Threading.Tasks;
using MetricsExample.DifferentNamespace;
using MetricsExample.Model;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Prometheus.Client;

namespace MetricsExample
{
    public class ServiceWithMetrics : INoOutputService<InputMessage>
    {
        private readonly IServiceInDifferentNamespace _serviceInDifferentNamespace;
        private readonly ICounter? _counter;
        private readonly ISummary? _simpleSummary;
        private readonly IMetricFamily<ISummary>? _labeledSummary;

        public ServiceWithMetrics(IMetricsFactory<ServiceWithMetrics> metricFactory,
            IServiceInDifferentNamespace serviceInDifferentNamespace)
        {
            _serviceInDifferentNamespace = serviceInDifferentNamespace;

            // Resulting label in Prometheus: metricsexample_emtpy_string_total
            _counter = metricFactory?.CreateCounter("empty_string_total",
                "Counts the total number of recieved empty strings.");

            // Resulting label in Prometheus: metricsexample_fancy_number
            _simpleSummary = metricFactory?.CreateSummary("fancy_number",
                "Shows the distribution of fancy numbers.");

            // Resulting label in Prometheus: metricsexample_processing_time
            _labeledSummary = metricFactory?.CreateSummary("processing_time",
                "Shows the processing time of fancy inputs.", new[] { "successful" });
        }

        public Task<ProcessedMessageStatus> HandleMessageAsync(MotorCloudEvent<InputMessage> inputEvent,
            CancellationToken token = default)
        {
            /*
             * Handle incoming messages
             * Get the input message from the cloud event
             */
            var input = inputEvent.TypedData;

            // Do your magic here .....
            try
            {
                MagicFunc(input);
            }
            catch (ThreadInterruptedException)
            {
                return Task.FromResult(ProcessedMessageStatus.TemporaryFailure);
            }

            return Task.FromResult(ProcessedMessageStatus.Success);
        }

        // Use metrics
        private void MagicFunc(InputMessage input)
        {
            if (input.FancyText == "")
            {
                _counter?.Inc();
            }

            _simpleSummary?.Observe(input.FancyNumber);

            var success = false;
            /*
             * With AutoObserveStopwatch, the metrics are still collected,
             * even if an exception is thrown within the using scope.
             * For histograms, AutoObserve can be used as a more generic approach.
             * 
             * When using the Rider IDE, you might see a warning here, regarding possible
             * modifications of the variable `success` in the outer scope. Because this is
             * exactly what we want to achieve here, you can simply ignore this warning.
             * Applying the fix suggested by Rider would even break the functionality.
             */
            using (new AutoObserveStopwatch(() => _labeledSummary?.WithLabels(success.ToString())))
            {
                success = LongRunningDangerousCheck(input);
            }

            _serviceInDifferentNamespace.CountInDifferentNamespace();
        }

        private static bool LongRunningDangerousCheck(InputMessage input)
        {
            Thread.Sleep(100);
            return input.FancyNumber < 5;
        }
    }
}
