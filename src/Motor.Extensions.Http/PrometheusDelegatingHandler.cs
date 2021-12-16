using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Prometheus.Client;

namespace Motor.Extensions.Http;

internal class PrometheusDelegatingHandler : DelegatingHandler
{
    private readonly IMetricFamily<ISummary> _requestLatency;
    private readonly IMetricFamily<ICounter> _requestTotal;

    public PrometheusDelegatingHandler(IMetricsFactory<PrometheusDelegatingHandler> metricsFactory)
    {
        _requestTotal =
            metricsFactory.CreateCounter("request_total", "number of external request", false, "host", "status");
        _requestLatency = metricsFactory.CreateSummary("request_latency", "request duration in ms", new[] { "host" });
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken token)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var response = await base.SendAsync(request, token);
        var uri = request.RequestUri;
        if (uri is null)
        {
            return response;
        }

        _requestTotal.WithLabels(uri.Host, response.StatusCode.ToString()).Inc();
        _requestLatency.WithLabels(uri.Host).Observe(stopwatch.ElapsedMilliseconds);
        return response;
    }
}
