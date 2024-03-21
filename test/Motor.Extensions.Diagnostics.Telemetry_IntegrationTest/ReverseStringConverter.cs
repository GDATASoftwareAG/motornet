using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Motor.Extensions.Conversion.Abstractions;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Diagnostics.Telemetry;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Prometheus.Client;

namespace Motor.Extensions.Diagnostics.Telemetry_IntegrationTest;

public class ReverseStringConverter : ISingleOutputService<string, string>
{
    private readonly ILogger<ReverseStringConverter> _logger;
    private readonly IMetricFamily<ISummary> _summary;
    private static readonly ActivitySource ActivitySource = new(OpenTelemetryOptions.DefaultActivitySourceName);

    public ReverseStringConverter(ILogger<ReverseStringConverter> logger,
        IMetricsFactory<ReverseStringConverter> metricsFactory)
    {
        _logger = logger;
        _summary = metricsFactory.CreateSummary("summaryName", "summaryHelpString", new[] { "someLabel" });
    }

    public Task<MotorCloudEvent<string>?> ConvertMessageAsync(MotorCloudEvent<string> dataCloudEvent,
        CancellationToken token = default)
    {
        _logger.LogInformation("log your request");
        var tmpChar = dataCloudEvent.TypedData.ToCharArray();
        if (!ActivitySource.HasListeners())
        {
            throw new ArgumentException();
        }

        var reversed = tmpChar.Reverse().ToArray();
        _summary.WithLabels("collect_your_metrics").Observe(1.0);
        return Task.FromResult<MotorCloudEvent<string>?>(dataCloudEvent.CreateNew(new string(reversed)));
    }
}

public class StringSerializer : IMessageSerializer<string>
{
    public byte[] Serialize(string message)
    {
        return Encoding.UTF8.GetBytes(message);
    }
}

public class StringDeserializer : IMessageDeserializer<string>
{
    public string Deserialize(byte[] message)
    {
        return Encoding.UTF8.GetString(message);
    }
}
