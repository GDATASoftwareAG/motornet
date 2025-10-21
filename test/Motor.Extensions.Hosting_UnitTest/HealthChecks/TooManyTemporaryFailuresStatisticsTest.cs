using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.HealthChecks;
using Xunit;

namespace Motor.Extensions.Hosting_UnitTest.HealthChecks;

public class TooManyTemporaryFailuresStatisticsTest
{
    [Theory]
    [InlineData(ProcessedMessageStatus.Success)]
    [InlineData(ProcessedMessageStatus.InvalidInput)]
    public async Task RegisterMessageStatusAsync_StatusHandledCorrectly_SetLastHandledMessage(
        ProcessedMessageStatus status
    )
    {
        var tooManyTemporaryFailuresStatistics = new TooManyTemporaryFailuresStatistics<string>();
        var beforeRegister = DateTimeOffset.UtcNow;

        await tooManyTemporaryFailuresStatistics.RegisterMessageStatusAsync(status);

        Assert.InRange(tooManyTemporaryFailuresStatistics.LastHandledMessageAt, beforeRegister, DateTimeOffset.UtcNow);
    }

    [Theory]
    [InlineData(ProcessedMessageStatus.CriticalFailure)]
    [InlineData(ProcessedMessageStatus.TemporaryFailure)]
    public async Task RegisterMessageStatusAsync_StatusNotHandledCorrectly_DoesNotSetLastHandledMessage(
        ProcessedMessageStatus status
    )
    {
        var tooManyTemporaryFailuresStatistics = new TooManyTemporaryFailuresStatistics<string>();
        var beforeRegister = DateTimeOffset.UtcNow;

        await tooManyTemporaryFailuresStatistics.RegisterMessageStatusAsync(status);

        Assert.NotInRange(
            tooManyTemporaryFailuresStatistics.LastHandledMessageAt,
            beforeRegister,
            DateTimeOffset.UtcNow
        );
    }

    [Theory]
    [InlineData(1, ProcessedMessageStatus.TemporaryFailure)]
    [InlineData(4, ProcessedMessageStatus.TemporaryFailure)]
    [InlineData(1, ProcessedMessageStatus.CriticalFailure)]
    [InlineData(4, ProcessedMessageStatus.CriticalFailure)]
    public async Task RegisterMessageStatusAsync_StatusNotHandledCorrectly_IncreasesTemporaryFailureCount(
        uint count,
        ProcessedMessageStatus status
    )
    {
        var tooManyTemporaryFailuresStatistics = new TooManyTemporaryFailuresStatistics<string>();

        for (uint i = 0; i < count; i++)
        {
            await tooManyTemporaryFailuresStatistics.RegisterMessageStatusAsync(status);
        }

        Assert.Equal(count, tooManyTemporaryFailuresStatistics.TemporaryFailureCountSinceLastHandledMessage);
    }

    [Theory]
    [InlineData(ProcessedMessageStatus.Success)]
    [InlineData(ProcessedMessageStatus.InvalidInput)]
    public async Task RegisterMessageStatusAsync_StatusHandledCorrectly_ResetTemporaryFailureCount(
        ProcessedMessageStatus status
    )
    {
        var tooManyTemporaryFailuresStatistics = new TooManyTemporaryFailuresStatistics<string>
        {
            TemporaryFailureCountSinceLastHandledMessage = 100,
        };

        await tooManyTemporaryFailuresStatistics.RegisterMessageStatusAsync(status);

        Assert.Equal((uint)0, tooManyTemporaryFailuresStatistics.TemporaryFailureCountSinceLastHandledMessage);
    }

    [Fact]
    public async Task RegisterMessageStatusAsync_ManyStatusesNotHandledCorrectlyInParallel_CorrectTemporaryFailureCount()
    {
        var tooManyTemporaryFailuresStatistics = new TooManyTemporaryFailuresStatistics<string>();
        var notHandledCorrectlyTasks = new List<Task>(100);
        var startSignal = new SemaphoreSlim(0, 100);
        for (var i = 0; i < 100; i++)
        {
            notHandledCorrectlyTasks.Add(
                IncreaseTemporaryFailureCount(startSignal, tooManyTemporaryFailuresStatistics)
            );
        }
        startSignal.Release(100);

        await Task.WhenAll(notHandledCorrectlyTasks);

        Assert.Equal((uint)100, tooManyTemporaryFailuresStatistics.TemporaryFailureCountSinceLastHandledMessage);
    }

    private static async Task IncreaseTemporaryFailureCount(
        SemaphoreSlim startSignal,
        TooManyTemporaryFailuresStatistics<string> tooManyTemporaryFailuresStatistics
    )
    {
        await startSignal.WaitAsync();

        await tooManyTemporaryFailuresStatistics.RegisterMessageStatusAsync(ProcessedMessageStatus.TemporaryFailure);
    }
}
