using System.Collections.Generic;
using System.Linq;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Core;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.Compression.Abstractions
{
    public static class CompressionTypeExtension
    {
        public static CloudEventAttribute CompressionTypeAttribute { get; } =
            CloudEventAttribute.CreateExtension("compressiontype", CloudEventAttributeType.String);

        public static IEnumerable<CloudEventAttribute> AllAttributes { get; } =
            new[] { CompressionTypeAttribute }.ToList().AsReadOnly();

        public static MotorCloudEvent<TData> SetCompressionType<TData>(this MotorCloudEvent<TData> cloudEvent, string? value) where TData : class
        {
            Validation.CheckNotNull(cloudEvent, nameof(cloudEvent));
            cloudEvent[CompressionTypeAttribute] = value;
            return cloudEvent;
        }

        public static string GetCompressionType<TData>(this MotorCloudEvent<TData> cloudEvent) where TData : class =>
            (string?)Validation.CheckNotNull(cloudEvent, nameof(cloudEvent))[CompressionTypeAttribute] ??
            NoOpMessageCompressor.NoOpCompressionType;
    }
}
