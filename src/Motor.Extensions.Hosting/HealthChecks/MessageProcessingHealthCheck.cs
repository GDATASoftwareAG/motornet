using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Hosting.HealthChecks
{
    public class MessageProcessingHealthCheckConfig
    {
        public TimeSpan MaxTimeSinceLastProcessedMessage { get; set; } = TimeSpan.FromMinutes(5);
    }

    public class MessageProcessingHealthCheck<TInput> : IHealthCheck where TInput : class
    {
        private readonly TimeSpan _maxTimeWithoutAcknowledgedMessage;
        private readonly IBackgroundTaskQueue<MotorCloudEvent<TInput>> _queue;

        public MessageProcessingHealthCheck(IOptions<MessageProcessingHealthCheckConfig> config,
            IBackgroundTaskQueue<MotorCloudEvent<TInput>> queue)
        {
            _maxTimeWithoutAcknowledgedMessage = config.Value.MaxTimeSinceLastProcessedMessage;
            _queue = queue;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken token = default)
        {
            if (_queue.ItemCount == 0) return Task.FromResult(HealthCheckResult.Healthy());

            return Task.FromResult(DateTimeOffset.UtcNow - _queue.LastDequeuedAt > _maxTimeWithoutAcknowledgedMessage
                ? HealthCheckResult.Unhealthy()
                : HealthCheckResult.Healthy());
        }
    }
}
