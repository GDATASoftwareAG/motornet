using System;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.Hosting.NATS;

internal static class NATSClientExtension
{
    public static MotorCloudEvent<byte[]> ToMotorCloudEvent(this byte[] message,
        IApplicationNameService applicationNameService)
    {
        var motorCloudEvent = new MotorCloudEvent<byte[]>(applicationNameService,
            message, new Uri("nats://notset"));
        return motorCloudEvent;
    }
}
