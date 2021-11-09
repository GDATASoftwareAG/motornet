using System;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Hosting.HealthChecks;

// ReSharper disable once UnusedTypeParameter
public class TooManyTemporaryFailuresStatistics<TInput>
{
    public DateTimeOffset LastHandledMessageAt { get; set; } = DateTimeOffset.MinValue;
    public uint TemporaryFailureCountSinceLastHandledMessage { get; set; }

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task RegisterMessageStatusAsync(ProcessedMessageStatus lastStatus)
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            switch (lastStatus)
            {
                case ProcessedMessageStatus.Success:
                case ProcessedMessageStatus.InvalidInput:
                    LastHandledMessageAt = DateTimeOffset.UtcNow;
                    TemporaryFailureCountSinceLastHandledMessage = 0;
                    break;
                case ProcessedMessageStatus.CriticalFailure:
                case ProcessedMessageStatus.TemporaryFailure:
                    TemporaryFailureCountSinceLastHandledMessage++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lastStatus), lastStatus, null);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
