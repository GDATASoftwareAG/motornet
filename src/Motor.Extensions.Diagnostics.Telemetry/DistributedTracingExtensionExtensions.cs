using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Core;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.Diagnostics.Telemetry;

public static class DistributedTracingExtension
{
    public static CloudEventAttribute TraceParentAttribute { get; } =
        CloudEventAttribute.CreateExtension("traceparent", CloudEventAttributeType.String);

    public static CloudEventAttribute TraceStateAttribute { get; } =
        CloudEventAttribute.CreateExtension("tracestate", CloudEventAttributeType.String);

    public static IEnumerable<CloudEventAttribute> AllAttributes { get; } =
        new[] { TraceParentAttribute, TraceStateAttribute }.ToList().AsReadOnly();

    extension<TData>(MotorCloudEvent<TData> cloudEvent)
    {
        public void SetActivity(Activity activity)
        {
            Validation.CheckNotNull(cloudEvent, nameof(cloudEvent));
            cloudEvent[TraceParentAttribute] = activity.Id;
            if (!string.IsNullOrWhiteSpace(activity.TraceStateString))
            {
                cloudEvent[TraceStateAttribute] = activity.TraceStateString;
            }
        }

        public ActivityContext GetActivityContext()
        {
            if (cloudEvent[TraceParentAttribute] is not string traceParent)
            {
                return default;
            }

            var traceState = cloudEvent[TraceStateAttribute] as string;
            return ActivityContext.Parse(traceParent, traceState);
        }
    }
}
