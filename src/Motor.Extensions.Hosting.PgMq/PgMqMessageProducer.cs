using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Options;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.PgMq.Options;
using Npgmq;

namespace Motor.Extensions.Hosting.PgMq;

/// <summary>
/// Message producer for pgmq (https://github.com/pgmq/pgmq).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Uses Npgmq (https://github.com/brianpursley/Npgmq).</item>
/// </list>
/// </remarks>
/// <typeparam name="TOutput">The output message type.</typeparam>
public class PgMqMessageProducer<TOutput> : IRawMessagePublisher<TOutput>, IAsyncDisposable
    where TOutput : notnull
{
    private readonly PgMqPublisherOptions<TOutput> _options;
    private readonly PublisherOptions _publisherOptions;
    private readonly CloudEventFormatter _cloudEventFormatter;
    private readonly INpgmqClient _npgmqClient;

    public PgMqMessageProducer(
        IOptions<PgMqPublisherOptions<TOutput>> options,
        IOptions<PublisherOptions> publisherOptions,
        CloudEventFormatter cloudEventFormatter,
        INpgmqClient npgmqClient
    )
    {
        _options = options.Value;
        _publisherOptions = publisherOptions.Value;
        _cloudEventFormatter = cloudEventFormatter;
        _npgmqClient = npgmqClient;
    }

    /// <summary>
    /// Starts the <see cref="PgMqMessageProducer{TOutput}"/>.
    /// </summary>
    /// <param name="token">A cancellation token.</param>
    /// <returns>A <see cref="Task"/> that completes when the producer is ready.</returns>
    /// <exception cref="NpgmqException">Thrown if the connection to Postgres fails or the pgmq extension cannot be initialised.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Ensures the pgmq extension has been created in Postgres.</item>
    /// <item>Creates the queue if it does not exist.</item>
    /// </list>
    /// </remarks>
    public async Task StartAsync(CancellationToken token = default)
    {
        await _npgmqClient.InitAsync(token);
        await _npgmqClient.CreateQueueAsync(_options.QueueName, token);
    }

    /// <summary>
    /// Publishes a <see cref="MotorCloudEvent{TData}"/> to the pgmq queue.
    /// </summary>
    /// <param name="motorCloudEvent">The cloud event to publish.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>A <see cref="Task"/> that completes when the message has been added to the database.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item>
    /// In Protocol format (CloudEvents Binary Content Mode), CloudEvent attributes are written as Npgmq message
    /// headers and the raw payload bytes are sent as the message body.
    /// </item>
    /// <item>
    /// In JSON format (CloudEvents Structured Content Mode), the full CloudEvent – including all attributes and
    /// the payload – is encoded as a single JSON envelope. No separate headers are used.
    /// </item>
    /// </list>
    /// </remarks>
    public async Task PublishMessageAsync(MotorCloudEvent<byte[]> motorCloudEvent, CancellationToken token = default)
    {
        switch (_publisherOptions.CloudEventFormat)
        {
            case CloudEventFormat.Protocol:
                var headers = BuildHeaders(motorCloudEvent);
                await _npgmqClient.SendAsync(_options.QueueName, motorCloudEvent.TypedData, headers, token);
                break;
            case CloudEventFormat.Json:
                var jsonBytes = _cloudEventFormatter.EncodeStructuredModeMessage(
                    motorCloudEvent.ConvertToCloudEvent(),
                    out _
                );
                // Convert ReadOnlyMemory<byte> to string so that NpgmqClient stores the JSON as-is (JSONB).
                // Passing the raw bytes directly would cause Npgmq to Base64-encode them via JsonSerializer.
                var json = Encoding.UTF8.GetString(jsonBytes.ToArray());
                await _npgmqClient.SendAsync(_options.QueueName, json, token);
                break;
            default:
                throw new UnhandledCloudEventFormatException(_publisherOptions.CloudEventFormat);
        }
    }

    private static System.Collections.Generic.Dictionary<string, object> BuildHeaders(
        MotorCloudEvent<byte[]> motorCloudEvent
    ) =>
        motorCloudEvent
            .GetPopulatedAttributes()
            .ToDictionary(kvp => kvp.Key.Name, kvp => (object)kvp.Key.Format(kvp.Value));

    public ValueTask DisposeAsync() => _npgmqClient is IAsyncDisposable d ? d.DisposeAsync() : ValueTask.CompletedTask;
}
