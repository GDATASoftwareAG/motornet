using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Prometheus.Client;

namespace Motor.Extensions.Hosting.Bridge.Internals;

public class PassThoughService : ISingleOutputService<ByteData, ByteData>
{
    private readonly ISummary summary;

    public PassThoughService(IMetricsFactory<PassThoughService> metricsFactory)
    {
        summary = metricsFactory.CreateSummary("message_pass_tough_bytes", "");
    }

    public Task<MotorCloudEvent<ByteData>?> ConvertMessageAsync(MotorCloudEvent<ByteData> dataCloudEvent,
        CancellationToken token = default)
    {
        summary.Observe(dataCloudEvent.TypedData?.data.Length ?? 0);
        return Task.FromResult<MotorCloudEvent<ByteData>?>(dataCloudEvent.CreateNew(dataCloudEvent.TypedData, true));
    }
}
