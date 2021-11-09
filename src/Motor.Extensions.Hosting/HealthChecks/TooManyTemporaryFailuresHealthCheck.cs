using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Motor.Extensions.Hosting.HealthChecks;

public class TooManyTemporaryFailuresHealthCheck<TInput> : IHealthCheck where TInput : class
{
    private readonly TooManyTemporaryFailuresStatistics<TInput> _statistics;
    private readonly TooManyTemporaryFailuresOptions _options;

    public TooManyTemporaryFailuresHealthCheck(IOptions<TooManyTemporaryFailuresOptions> options,
        TooManyTemporaryFailuresStatistics<TInput> statistics)
    {
        _statistics = statistics;
        _options = options.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_statistics.TemporaryFailureCountSinceLastHandledMessage >
            _options.MaxTemporaryFailuresSinceLastHandledMessage && _statistics.LastHandledMessageAt <
            DateTimeOffset.UtcNow - _options.MaxTimeSinceLastHandledMessage)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy());
        }
        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
