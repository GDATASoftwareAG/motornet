using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Hosting.HealthChecks
{
    public class TooManyTemporaryFailuresDelegatingMessageHandler<TInput> : DelegatingMessageHandler<TInput>
        where TInput : class
    {
        private readonly TooManyTemporaryFailuresStatistics<TInput> _statistics;

        public TooManyTemporaryFailuresDelegatingMessageHandler(
            TooManyTemporaryFailuresStatistics<TInput> statistics)
        {
            _statistics = statistics;
        }

        public override async Task<ProcessedMessageStatus> HandleMessageAsync(MotorCloudEvent<TInput> dataCloudEvent,
            CancellationToken token = default)
        {
            var processedMessageStatus = await base.HandleMessageAsync(dataCloudEvent, token).ConfigureAwait(false);
            await _statistics.RegisterMessageStatusAsync(processedMessageStatus).ConfigureAwait(false);
            return processedMessageStatus;
        }
    }
}
