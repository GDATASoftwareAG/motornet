using System.Collections.Generic;
using System.Text;
using CloudNative.CloudEvents;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Hosting.SQS
{
    internal static class SQSClientExtensions
    {
        public static MotorCloudEvent<byte[]> ToMotorCloudEvent(this string message,
            IApplicationNameService applicationNameService)
        {
            var motorCloudEvent = new MotorCloudEvent<byte[]>(applicationNameService, 
                Encoding.UTF8.GetBytes(message), new List<ICloudEventExtension>());
            return motorCloudEvent;
        }
    }
}
