using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
    private readonly PgMqPublisherOptions _options;
    private readonly ILogger<PgMqMessageProducer<TOutput>> _logger;
    private readonly PublisherOptions _publisherOptions;
    private readonly INpgmqClient _npgmqClient;

    public PgMqMessageProducer(
        IOptions<PgMqPublisherOptions> options,
        ILogger<PgMqMessageProducer<TOutput>> logger,
        IOptions<PublisherOptions> publisherOptions,
        INpgmqClient npgmqClient
    )
    {
        _options = options.Value;
        _logger = logger;
        _publisherOptions = publisherOptions.Value;
        _npgmqClient = npgmqClient;
    }

    /// <summary>
    /// Starts the <see cref="PgMqMessageProducer{TOutput}"/>.
    /// </summary>
    /// <param name="token">A cancellation token.</param>
    /// <returns>A <see cref="Task"/> that completes when the producer is ready.</returns>
    /// <exception cref="NpgmqException">
    /// Thrown if the Npgmq client fails to initialize or create the queue. This can occur due to database connectivity issues, missing pgmq extension in Postgres, invalid configuration, or other errors encountered by the underlying Npgmq client.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the operation is canceled via the provided <paramref name="token"/>.
    /// </exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Ensures the pgmq extension has been created in Postgres.</item>
    /// <item>Creates the queue if it does not exist.</item>
    /// </list>
    /// </remarks>
    public async Task StartAsync(CancellationToken token = default)
    {
        var retry = 0;
        while (true)
        {
            try
            {
                await _npgmqClient.InitAsync(token);
                break;
            }
            catch (NpgmqException e)
            {
                retry++;
                if (retry > 5)
                {
                    _logger.LogError(e, "Failed to initialize Npgmq client after {RetryCount} attempts.", retry);
                    throw;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken: token);
            }
        }

        await _npgmqClient.CreateQueueAsync(_options.QueueName, token);
    }

    /// <summary>
    /// Stops the <see cref="PgMqMessageProducer{TOutput}"/>.
    /// </summary>
    /// <param name="token">A cancellation token.</param>
    public Task StopAsync(CancellationToken token = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Publishes a <see cref="MotorCloudEvent{TData}"/> to the pgmq queue.
    /// </summary>
    /// <param name="motorCloudEvent">The cloud event to publish.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>A <see cref="Task"/> that completes when the message has been added to the database.</returns>
    /// <remarks>
    /// Only the protocol format (CloudEvents Binary Content Mode) is currently supported. CloudEvent attributes
    /// are written as Npgmq message headers, and the raw payload bytes are sent as the message body.
    /// The JSON mode is not yet supported by this producer.
    /// </remarks>
    /// <exception cref="UnhandledCloudEventFormatException">If an unsupported cloud event format is specified.</exception>
    public async Task PublishMessageAsync(MotorCloudEvent<byte[]> motorCloudEvent, CancellationToken token = default)
    {
        switch (_publisherOptions.CloudEventFormat)
        {
            case CloudEventFormat.Protocol:
                var headers = BuildHeaders(motorCloudEvent);
                await _npgmqClient.SendAsync(_options.QueueName, motorCloudEvent.TypedData, headers, token);
                break;
            case CloudEventFormat.Json:
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
}
