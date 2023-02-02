using System;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.HealthChecks;
using Xunit;

namespace Motor.Extensions.Hosting_UnitTest.HealthChecks;

public class TooManyTemporaryFailuresHealthCheckTest
{
    [Fact]
    public async void CheckHealthAsync_NoTemporaryFailure_ServiceHealthy()
    {
        var healthCheck = CreateHealthCheck(new TooManyTemporaryFailuresStatistics<string>
        {
            LastHandledMessageAt = DateTimeOffset.UtcNow,
            TemporaryFailureCountSinceLastHandledMessage = 0
        });

        var result = (await healthCheck.CheckHealthAsync(new HealthCheckContext())).Status;

        Assert.Equal(HealthStatus.Healthy, result);
    }

    [Fact]
    public async void CheckHealthAsync_ManyTemporaryFailuresAfterLastHandledMessage_ServiceIsUnhealthy()
    {
        var healthCheck = CreateHealthCheck(new TooManyTemporaryFailuresStatistics<string>
        {
            LastHandledMessageAt = DateTimeOffset.MinValue,
            TemporaryFailureCountSinceLastHandledMessage = 1001
        });

        var result = (await healthCheck.CheckHealthAsync(new HealthCheckContext())).Status;

        Assert.Equal(HealthStatus.Unhealthy, result);
    }

    [Fact]
    public async void CheckHealthAsync_SingleTemporaryFailureAfterRecentLastHandledMessage_ServiceIsHealthy()
    {
        var healthCheck = CreateHealthCheck(new TooManyTemporaryFailuresStatistics<string>
        {
            LastHandledMessageAt = DateTimeOffset.UtcNow,
            TemporaryFailureCountSinceLastHandledMessage = 1
        });

        var result = (await healthCheck.CheckHealthAsync(new HealthCheckContext())).Status;

        Assert.Equal(HealthStatus.Healthy, result);
    }

    [Fact]
    public async void CheckHealthAsync_SingleTemporaryFailureAfterLastHandledMessage_ServiceIsHealthy()
    {
        var healthCheck = CreateHealthCheck(new TooManyTemporaryFailuresStatistics<string>
        {
            LastHandledMessageAt = DateTimeOffset.MinValue,
            TemporaryFailureCountSinceLastHandledMessage = 1
        });

        var result = (await healthCheck.CheckHealthAsync(new HealthCheckContext())).Status;

        Assert.Equal(HealthStatus.Healthy, result);
    }

    [Fact]
    public async void CheckHealthAsync_ManyTemporaryFailureAfterRecentLastHandledMessage_ServiceIsHealthy()
    {
        var healthCheck = CreateHealthCheck(new TooManyTemporaryFailuresStatistics<string>
        {
            LastHandledMessageAt = DateTimeOffset.UtcNow,
            TemporaryFailureCountSinceLastHandledMessage = 1001
        });

        var result = (await healthCheck.CheckHealthAsync(new HealthCheckContext())).Status;

        Assert.Equal(HealthStatus.Healthy, result);
    }

    private static TooManyTemporaryFailuresHealthCheck<string> CreateHealthCheck(
        TooManyTemporaryFailuresStatistics<string> temporaryFailuresStatistics)
    {
        var options = new TooManyTemporaryFailuresOptions();
        return new TooManyTemporaryFailuresHealthCheck<string>(
            Options.Create(options), temporaryFailuresStatistics);
    }
}
