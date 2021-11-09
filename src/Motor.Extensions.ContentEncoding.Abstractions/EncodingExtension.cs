using System.Collections.Generic;
using System.Linq;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Core;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.ContentEncoding.Abstractions;

public static class EncodingExtension
{
    public static CloudEventAttribute EncodingAttribute { get; } =
        CloudEventAttribute.CreateExtension("contentencoding", CloudEventAttributeType.String);

    public static IEnumerable<CloudEventAttribute> AllAttributes { get; } =
        new[] { EncodingAttribute }.ToList().AsReadOnly();

    public static MotorCloudEvent<TData> SetEncoding<TData>(this MotorCloudEvent<TData> cloudEvent, string? value)
        where TData : class
    {
        Validation.CheckNotNull(cloudEvent, nameof(cloudEvent));
        cloudEvent[EncodingAttribute] = value;
        return cloudEvent;
    }

    public static string GetEncoding<TData>(this MotorCloudEvent<TData> cloudEvent) where TData : class =>
        Validation.CheckNotNull(cloudEvent, nameof(cloudEvent))[EncodingAttribute] as string ??
        NoOpMessageEncoder.NoEncoding;
}
