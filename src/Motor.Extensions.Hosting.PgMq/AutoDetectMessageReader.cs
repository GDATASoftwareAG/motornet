using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Npgmq;

namespace Motor.Extensions.Hosting.PgMq;

/// <summary>
/// Reads pgmq messages and dynamically detects the CloudEvents encoding:
/// <list type="bullet">
/// <item>If the message has pgmq headers containing CloudEvent attributes, it is decoded as <b>binary content mode</b> (protocol format).</item>
/// <item>Otherwise, the message body is decoded as a <b>structured-mode JSON envelope</b>.</item>
/// </list>
/// </summary>
internal sealed class AutoDetectMessageReader : IPgMqMessageReader
{
    private readonly CloudEventFormatter _cloudEventFormatter;
    private readonly IApplicationNameService _applicationNameService;

    public AutoDetectMessageReader(CloudEventFormatter cloudEventFormatter, IApplicationNameService applicationNameService)
    {
        _cloudEventFormatter = cloudEventFormatter;
        _applicationNameService = applicationNameService;
    }

    public async Task<PgMqMessage?> ReadAsync(
        INpgmqClient client,
        string queueName,
        int visibilityTimeoutInSeconds,
        int pollTimeoutSeconds,
        int pollIntervalMilliseconds,
        CancellationToken token
    )
    {
        // Always poll as string so we can handle both protocol and JSON format without
        // committing to a type parameter up front.
        var message = await client.PollAsync<string>(
            queueName,
            visibilityTimeoutInSeconds,
            pollTimeoutSeconds,
            pollIntervalMilliseconds,
            token
        );

        if (message is null)
        {
            return null;
        }

        var cloudEvent = message.Headers is { Count: > 0 }
            ? DecodeProtocolFormat(message.Message ?? string.Empty, message.Headers)
            : DecodeJsonFormat(message.Message ?? string.Empty);

        return new PgMqMessage(message.MsgId, cloudEvent);
    }

    /// <summary>
    /// Protocol (binary content mode): CloudEvent attributes are stored in pgmq headers,
    /// the body is the raw payload encoded as a Base64 string by Npgmq.
    /// </summary>
    private MotorCloudEvent<byte[]> DecodeProtocolFormat(
        string body,
        System.Collections.Generic.IReadOnlyDictionary<string, object> headers
    )
    {
        var bodyBytes = Convert.FromBase64String(body);
        var cloudEvent = new MotorCloudEvent<byte[]>(_applicationNameService, bodyBytes, new Uri("pgmq://notset"));

        foreach (var (key, value) in headers)
        {
            try
            {
                cloudEvent.SetAttributeFromString(key, value.ToString() ?? string.Empty);
            }
            catch (ArgumentException)
            {
                // Ignore headers that cannot be parsed as valid CloudEvent attributes.
            }
        }

        return cloudEvent;
    }

    /// <summary>
    /// JSON format (structured content mode): The entire CloudEvent (attributes + data) is encoded
    /// as a single JSON envelope as per the CloudEvents JSON Event Format specification.
    /// </summary>
    private MotorCloudEvent<byte[]> DecodeJsonFormat(string body)
    {
        var jsonBytes = Encoding.UTF8.GetBytes(body);
        var cloudEvent = _cloudEventFormatter.DecodeStructuredModeMessage(jsonBytes, null, null);
        return cloudEvent.ToMotorCloudEvent(_applicationNameService);
    }
}

