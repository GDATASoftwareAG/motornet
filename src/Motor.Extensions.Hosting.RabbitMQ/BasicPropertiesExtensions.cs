using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudNative.CloudEvents;
using Motor.Extensions.ContentEncoding.Abstractions;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using RabbitMQ.Client;

namespace Motor.Extensions.Hosting.RabbitMQ;

public static class BasicPropertiesExtensions
{
    public static string CloudEventPrefix => "cloudEvents:";

    // ReSharper disable once InconsistentNaming
    private static readonly Version Version_0_7_0 = new("0.7.0.0");
    private static readonly List<CloudEventAttribute> IgnoredAttributes = new();

    static BasicPropertiesExtensions()
    {
        IgnoredAttributes.AddRange(RabbitMQBindingExtension.AllAttributes);
        IgnoredAttributes.AddRange(RabbitMQPriorityExtension.AllAttributes);
        IgnoredAttributes.AddRange(EncodingExtension.AllAttributes);
        IgnoredAttributes.Add(MotorCloudEventInfo.SpecVersion.DataContentTypeAttribute);
    }

    public static void Update<T>(this IBasicProperties self, MotorCloudEvent<byte[]> cloudEvent,
        RabbitMQPublisherOptions<T> options, CloudEventFormat format)
    {
        var messagePriority = cloudEvent.GetRabbitMQPriority() ?? options.DefaultPriority;
        if (messagePriority.HasValue)
            self.Priority = messagePriority.Value;

        if (format == CloudEventFormat.Json)
        {
            return;
        }

        self.ContentEncoding = cloudEvent.GetEncoding();
        self.ContentType = cloudEvent.ContentType;

        var headers = new Dictionary<string, object>();

        var attributesToConsider = cloudEvent.GetPopulatedAttributes()
            .Where(t => !IgnoredAttributes.Contains(t.Key));

        foreach (var (key, value) in attributesToConsider)
        {
            headers.Add($"{CloudEventPrefix}{key.Name}", Encoding.UTF8.GetBytes(key.Format(value)));
        }

        self.Headers = headers;
    }

    public static MotorCloudEvent<byte[]> ExtractCloudEvent(this IBasicProperties self,
        IApplicationNameService applicationNameService, ReadOnlyMemory<byte> body)
    {
        var attributes = new Dictionary<string, object>();
        IDictionary<string, object> headers = new Dictionary<string, object>();
        if (self.IsHeadersPresent() && self.Headers is not null)
        {
            headers = self.Headers;
        }

        var cloudEventHeaders = headers
            .Where(t => t.Key.StartsWith(CloudEventPrefix))
            .Select(t => (Key: t.Key[CloudEventPrefix.Length..], t.Value))
            .Where(t => !t.Key.Equals(CloudEventsSpecVersion.SpecVersionAttribute.Name, StringComparison.InvariantCultureIgnoreCase));

        foreach (var (key, value) in cloudEventHeaders)
        {
            attributes.Add(key, value);
        }

        var cloudEvent = new MotorCloudEvent<byte[]>(applicationNameService, body.ToArray(),
            null, new Uri("rabbitmq://notset"), null, null, self.ContentType);

        if (self.IsPriorityPresent())
        {
            cloudEvent.SetRabbitMQPriority(self.Priority);
        }

        cloudEvent.SetEncoding(self.ContentEncoding);

        if (attributes.Count == 0)
        {
            return cloudEvent;
        }

        var hasVersion =
            attributes.TryGetValue(MotorVersionExtension.MotorVersionAttribute.Name, out var versionObject);
        Version? version = null;
        if (hasVersion && versionObject is byte[] versionBytes)
        {
            var versionString = Encoding.UTF8.GetString(versionBytes);
            // If the version is enclosed in double quotes, that means it has passed an old Motor.NET version as an
            // unknown header. Therefore, the version number should not be trusted and instead, the version is set
            // to null.
            version = versionString.StartsWith("\"") || versionString.EndsWith("\"") ? null : new Version(
                Encoding.UTF8.GetString(versionBytes));
        }

        foreach (var (key, value) in attributes)
        {
            if (value is not byte[] byteValue)
            {
                continue;
            }
            var decoded = Encoding.UTF8.GetString(byteValue);
            if ((version is null || version < Version_0_7_0)
                && decoded.StartsWith("\"") && decoded.EndsWith("\""))
            {
                decoded = decoded.Substring(1, decoded.Length - 2);
            }
            cloudEvent.SetAttributeFromString(key.ToLowerInvariant(), decoded);
        }

        return cloudEvent;
    }
}
