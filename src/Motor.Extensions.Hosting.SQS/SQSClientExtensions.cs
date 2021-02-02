using System;
using System.Text;
using Motor.Extensions.Hosting.Abstractions;

namespace Motor.Extensions.Hosting.SQS
{
    internal static class SQSClientExtensions
    {
        public static MotorCloudEvent<byte[]> ToMotorCloudEvent(this string message,
            IApplicationNameService applicationNameService)
        {
            var motorCloudEvent = new MotorCloudEvent<byte[]>(applicationNameService,
                Encoding.UTF8.GetBytes(message), "String", new Uri("sqs://notset"));
            return motorCloudEvent;
        }
    }
}
