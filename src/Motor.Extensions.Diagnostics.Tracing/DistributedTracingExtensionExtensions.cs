using System;
using System.Diagnostics;
using CloudNative.CloudEvents.Extensions;

namespace Motor.Extensions.Diagnostics.Tracing
{
    public static class DistributedTracingExtensionExtensions
    {
        [Obsolete("Replaced by DistributedTracingExtensionExtensions")]
        public static void SetActivity(this DistributedTracingExtension extension, Activity activity)
        {
            Telemetry.DistributedTracingExtensionExtensions.SetActivity(extension, activity);
        }

        [Obsolete("Replaced by DistributedTracingExtensionExtensions")]
        public static ActivityContext GetActivityContext(this DistributedTracingExtension extension)
        {
            return Telemetry.DistributedTracingExtensionExtensions
                .GetActivityContext(extension);
        }

    }
}
