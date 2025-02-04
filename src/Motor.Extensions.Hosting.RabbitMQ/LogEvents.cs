using Microsoft.Extensions.Logging;

namespace Motor.Extensions.Hosting.RabbitMQ;

public static class LogEvents
{
    public static readonly EventId UnexpectedErrorOnConsume = new(0, nameof(UnexpectedErrorOnConsume));
    public static readonly EventId CriticalFailureOnConsume = new(1, nameof(CriticalFailureOnConsume));
    public static readonly EventId QueueStateRetrieval = new(2, nameof(QueueStateRetrieval));
    public static readonly EventId QueueStateRetrievalFailed = new(3, nameof(QueueStateRetrievalFailed));
    public static readonly EventId ChannelNullAfterProcessingComplete =
        new(4, nameof(ChannelNullAfterProcessingComplete));
    public static readonly EventId ConsumerNotStarted = new(5, nameof(ConsumerNotStarted));
}
