using System;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.HealthChecks;
using Xunit;

namespace Motor.Extensions.Hosting_UnitTest.HealthChecks;

public class MessageProcessingHealthCheckTest
{
    [Fact]
    public async void CheckHealthAsync_LastDequeuedAtExceededTimeoutRangeAndQueueNotEmpty_ServiceIsUnhealthy()
    {
        var healthCheck = CreateHealthCheck(true, 10);

        var result = (await healthCheck.CheckHealthAsync(new HealthCheckContext())).Status;

        Assert.Equal(HealthStatus.Unhealthy, result);
    }

    [Fact]
    public async void CheckHealthAsync_LastDequeuedAtWithinTimeoutRangeAndQueueNotEmpty_ServiceIsHealthy()
    {
        var healthCheck = CreateHealthCheck(false, 10);

        var result = (await healthCheck.CheckHealthAsync(new HealthCheckContext())).Status;

        Assert.Equal(HealthStatus.Healthy, result);
    }

    [Fact]
    public async void CheckHealthAsync_LastDequeuedAtExceededTimeoutRangeAndQueueEmpty_ServiceIsHealthy()
    {
        var healthCheck = CreateHealthCheck(true, 0);

        var result = (await healthCheck.CheckHealthAsync(new HealthCheckContext())).Status;

        Assert.Equal(HealthStatus.Healthy, result);
    }

    [Fact]
    public async void CheckHealthAsync_LastDequeuedAtWithinTimeoutRangeAndQueueEmpty_ServiceIsHealthy()
    {
        var healthCheck = CreateHealthCheck(false, 0);

        var result = (await healthCheck.CheckHealthAsync(new HealthCheckContext())).Status;

        Assert.Equal(HealthStatus.Healthy, result);
    }

    private MessageProcessingHealthCheck<string> CreateHealthCheck(bool exceededMaxTimeSinceLastProcessedMessage,
        int itemCount)
    {
        var maxTimeSinceLastProcessedMessage = TimeSpan.FromMilliseconds(100);
        var config = new MessageProcessingOptions
        {
            MaxTimeSinceLastProcessedMessage = maxTimeSinceLastProcessedMessage
        };
        var queue = new Mock<IBackgroundTaskQueue<MotorCloudEvent<string>>>();
        queue.Setup(q => q.LastDequeuedAt).Returns(exceededMaxTimeSinceLastProcessedMessage
            ? DateTimeOffset.UtcNow.Subtract(maxTimeSinceLastProcessedMessage + TimeSpan.FromMilliseconds(50))
            : DateTimeOffset.UtcNow.Subtract(maxTimeSinceLastProcessedMessage - TimeSpan.FromMilliseconds(50)));
        queue.Setup(q => q.ItemCount).Returns(itemCount);
        return new MessageProcessingHealthCheck<string>(
            Options.Create(config),
            queue.Object);
    }
}
