using System;
using System.Text;
using Motor.Extensions.Hosting.CloudEvents;

namespace Motor.Extensions.Hosting.SQS
{
    internal static class SQSClientExtensions
    {
        public static MotorCloudEvent<byte[]> ToMotorCloudEvent(this string message,
            IApplicationNameService applicationNameService)
        {
            var motorCloudEvent = new MotorCloudEvent<byte[]>(applicationNameService,
                Encoding.UTF8.GetBytes(message), new Uri("sqs://notset"));
            return motorCloudEvent;
        }
    }
}
