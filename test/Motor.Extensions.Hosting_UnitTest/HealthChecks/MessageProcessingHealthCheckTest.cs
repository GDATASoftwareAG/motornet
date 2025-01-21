using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.HealthChecks;
using Motor.Extensions.Hosting.Internal;
using Motor.Extensions.TestUtilities;
using Xunit;

namespace Motor.Extensions.Hosting_UnitTest.HealthChecks;

public class MessageProcessingHealthCheckTest
{
    private readonly TimeSpan _timeout = TimeSpan.FromMilliseconds(50);

    [Fact]
    public async Task CheckHealthAsync_QueueHasMessagesWithoutRecentAcknowledgement_ServiceIsUnhealthy()
    {
        var queue = CreateEmptyQueue();
        var healthCheck = CreateHealthCheck(queue);
        queue.QueueBackgroundWorkItem(MotorCloudEvent.CreateTestCloudEvent<string>("message"));
        await Task.Delay(_timeout * 2);

        var result = (await healthCheck.CheckHealthAsync(new HealthCheckContext())).Status;

        Assert.Equal(HealthStatus.Unhealthy, result);
    }

    [Fact]
    public async Task CheckHealthAsync_QueueHasMessagesButMessageWasRecentlyAcknowledged_ServiceIsHealthy()
    {
        var queue = CreateEmptyQueue();
        var healthCheck = CreateHealthCheck(queue);
        queue.QueueBackgroundWorkItem(MotorCloudEvent.CreateTestCloudEvent<string>("message0"));
        await queue.DequeueAsync(CancellationToken.None);
        queue.QueueBackgroundWorkItem(MotorCloudEvent.CreateTestCloudEvent<string>("message1"));

        var result = (await healthCheck.CheckHealthAsync(new HealthCheckContext())).Status;

        Assert.Equal(HealthStatus.Healthy, result);
    }

    [Fact]
    public async Task CheckHealthAsync_QueueRemainsEmptyLongerThanTimeout_ServiceIsHealthy()
    {
        var queue = CreateEmptyQueue();
        var healthCheck = CreateHealthCheck(queue);
        await Task.Delay(_timeout * 2);

        var result = (await healthCheck.CheckHealthAsync(new HealthCheckContext())).Status;

        Assert.Equal(HealthStatus.Healthy, result);
    }

    [Fact]
    public async Task CheckHealthAsync_QueueBecomesEmptyLongerThanTimeout_ServiceIsHealthy()
    {
        var queue = CreateEmptyQueue();
        var healthCheck = CreateHealthCheck(queue);
        queue.QueueBackgroundWorkItem(MotorCloudEvent.CreateTestCloudEvent<string>("message"));
        await queue.DequeueAsync(CancellationToken.None);
        await Task.Delay(_timeout * 2);

        var result = (await healthCheck.CheckHealthAsync(new HealthCheckContext())).Status;

        Assert.Equal(HealthStatus.Healthy, result);
    }

    [Fact]
    public async Task CheckHealthAsync_QueueIsEmptyAndMessageWasRecentlyAcknowledged_ServiceIsHealthy()
    {
        var queue = CreateEmptyQueue();
        var healthCheck = CreateHealthCheck(queue);
        queue.QueueBackgroundWorkItem(MotorCloudEvent.CreateTestCloudEvent<string>("message"));
        await queue.DequeueAsync(CancellationToken.None);

        var result = (await healthCheck.CheckHealthAsync(new HealthCheckContext())).Status;

        Assert.Equal(HealthStatus.Healthy, result);
    }

    [Fact]
    public async Task CheckHealthAsync_MessageAppearsInQueueAfterQueueHasBeenEmpty_ServiceIsHealthy()
    {
        var queue = CreateEmptyQueue();
        var healthCheck = CreateHealthCheck(queue);
        await Task.Delay(_timeout * 2);
        queue.QueueBackgroundWorkItem(MotorCloudEvent.CreateTestCloudEvent<string>("message"));

        var result = (await healthCheck.CheckHealthAsync(new HealthCheckContext())).Status;

        Assert.Equal(HealthStatus.Healthy, result);
    }

    [Fact]
    public async Task CheckHealthAsync_MessageRemainsInQueueLongerThanTimeout_ServiceIsUnhealthy()
    {
        var queue = CreateEmptyQueue();
        var healthCheck = CreateHealthCheck(queue);
        queue.QueueBackgroundWorkItem(MotorCloudEvent.CreateTestCloudEvent<string>("message"));
        await Task.Delay(_timeout * 2);

        var result = (await healthCheck.CheckHealthAsync(new HealthCheckContext())).Status;

        Assert.Equal(HealthStatus.Unhealthy, result);
    }

    private MessageProcessingHealthCheck<string> CreateHealthCheck(IBackgroundTaskQueue<MotorCloudEvent<string>> queue)
    {
        var config = new MessageProcessingOptions
        {
            MaxTimeSinceLastProcessedMessage = _timeout
        };
        return new MessageProcessingHealthCheck<string>(Options.Create(config), queue);
    }

    private IBackgroundTaskQueue<MotorCloudEvent<string>> CreateEmptyQueue()
    {
        var queue = new BackgroundTaskQueue<MotorCloudEvent<string>>(null);
        return queue;
    }
}
