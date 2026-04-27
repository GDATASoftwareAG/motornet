using System;
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
public class PgMqMessageProducer<TOutput> : IRawMessagePublisher<TOutput>
    where TOutput : notnull
{
    private readonly PgMqPublisherOptions<TOutput> _options;
    private readonly PublisherOptions _publisherOptions;
    private readonly CloudEventFormatter _cloudEventFormatter;
    private NpgmqClient? _npgmqClient;

    public PgMqMessageProducer(
        IOptions<PgMqPublisherOptions<TOutput>> options,
        IOptions<PublisherOptions> publisherOptions,
        CloudEventFormatter cloudEventFormatter
    )
    {
        _options = options.Value;
        _publisherOptions = publisherOptions.Value;
        _cloudEventFormatter = cloudEventFormatter;
    }

    /// <summary>
    /// Starts the <see cref="PgMqMessageProducer{TOutput}"/>.
    /// </summary>
    /// <param name="token">A cancellation token.</param>
    /// <returns>A <see cref="Task"/> that completes when the producer is ready.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Creates the <see cref="NpgmqClient"/>.</item>
    /// <item>Ensures the pgmq extension has been created in Postgres.</item>
    /// <item>Creates the queue if it does not exist.</item>
    /// </list>
    /// </remarks>
    public async Task StartAsync(CancellationToken token = default)
    {
        _npgmqClient = new NpgmqClient(_options.ConnectionString);
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
    /// <item>In Protocol format, CloudEvent attributes are written as Npgmq message headers and the raw bytes are sent as the message body.</item>
    /// <item>In JSON format, the full CloudEvent is encoded as a structured JSON message.</item>
    /// </list>
    /// </remarks>
    public async Task PublishMessageAsync(MotorCloudEvent<byte[]> motorCloudEvent, CancellationToken token = default)
    {
        if (_npgmqClient is null)
        {
            throw new InvalidOperationException("Producer is not started. Call StartAsync first.");
        }

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
                var json = Encoding.UTF8.GetString(jsonBytes.ToArray());
                await _npgmqClient.SendAsync(_options.QueueName, json, token);
                break;
            default:
                throw new UnhandledCloudEventFormatException(_publisherOptions.CloudEventFormat);
        }
    }

    private static System.Collections.Generic.IReadOnlyDictionary<string, object> BuildHeaders(
        MotorCloudEvent<byte[]> motorCloudEvent
    )
    {
        var headers = new System.Collections.Generic.Dictionary<string, object>();
        foreach (var (attribute, value) in motorCloudEvent.GetPopulatedAttributes())
        {
            headers[attribute.Name] = attribute.Format(value);
        }
        return headers;
    }
}
