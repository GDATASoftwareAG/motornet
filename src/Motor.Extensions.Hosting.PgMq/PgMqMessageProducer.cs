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
/// Message producer for pgmq (https://github.com/pgmq/pgmq)
/// </summary>
/// <remarks>
/// <list>
/// <item>Uses Npgmq (https://github.com/brianpursley/Npgmq)</item>
/// </list>
/// </remarks>
/// <typeparam name="TOutput"></typeparam>
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
    /// Starts the PgMqMessageProducer
    /// </summary>
    /// <param name="token">A CancellationToken</param>
    /// <returns></returns>
    /// <remarks>
    /// * Creates the NpmqClient
    /// * Ensures the pgmq extension has been created in postgres
    /// * Creates the queue
    /// </remarks>
    public async Task StartAsync(CancellationToken token = default)
    {
        _npgmqClient = new NpgmqClient(_options.ConnectionString);
        await _npgmqClient.InitAsync(token);
        await _npgmqClient.CreateQueueAsync(_options.QueueName, token);
    }

    /// <summary>
    /// Publishes a MotorCloudEvent to a pgmq queue.
    /// </summary>
    /// <param name="motorCloudEvent"></param>
    /// <param name="token"></param>
    /// <returns>A Task that completes when the motorCloudEvent has been added to the DB.</returns>
    /// <remarks>
    /// <list>
    /// <item>Publishes headers and data of the motorCloudEvents.</item>
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
