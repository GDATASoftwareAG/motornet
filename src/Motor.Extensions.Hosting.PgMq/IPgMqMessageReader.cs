using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.CloudEvents;
using Npgmq;
namespace Motor.Extensions.Hosting.PgMq;
/// <summary>
/// Reads a single message from a pgmq queue and decodes it into a <see cref="MotorCloudEvent{TData}"/>.
/// </summary>
public interface IPgMqMessageReader
{
    /// <summary>
    /// Waits for the next message from the queue using long-polling.
    /// </summary>
    /// <param name="client">The <see cref="NpgmqClient"/> to use.</param>
    /// <param name="queueName">The queue to read from.</param>
    /// <param name="visibilityTimeoutInSeconds">Visibility timeout in seconds.</param>
    /// <param name="pollTimeoutSeconds">Maximum time in seconds to wait for a message before returning <c>null</c>.</param>
    /// <param name="pollIntervalMilliseconds">Interval in milliseconds between polling attempts within the long-poll window.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>
    /// A <see cref="PgMqMessage"/> containing the decoded <see cref="MotorCloudEvent{TData}"/> and the message id,
    /// or <c>null</c> if no message arrived within the poll timeout.
    /// </returns>
    Task<PgMqMessage?> ReadAsync(
        INpgmqClient client,
        string queueName,
        int visibilityTimeoutInSeconds,
        int pollTimeoutSeconds,
        int pollIntervalMilliseconds,
        CancellationToken token
    );
}
/// <summary>
/// A decoded pgmq message ready for processing.
/// </summary>
/// <param name="MsgId">The pgmq message id (used for deletion/acknowledgement).</param>
/// <param name="CloudEvent">The decoded <see cref="MotorCloudEvent{TData}"/>.</param>
public record PgMqMessage(long MsgId, MotorCloudEvent<byte[]> CloudEvent);
