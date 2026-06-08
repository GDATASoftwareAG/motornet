using System;
using Motor.Extensions.Hosting.CloudEvents;
using RabbitMQ.Client.Events;

namespace Motor.Extensions.Hosting.RabbitMQ;

public static class BasicDeliverEventArgsExtensions
{
    extension(BasicDeliverEventArgs self)
    {
        public MotorCloudEvent<byte[]> ExtractCloudEvent(
            IApplicationNameService applicationNameService,
            ReadOnlyMemory<byte> body,
            bool extractBindingKey
        )
        {
            var cloudEvent = self.BasicProperties.ExtractCloudEvent(applicationNameService, body);

            if (extractBindingKey)
            {
                cloudEvent.SetRabbitMQBinding(self.Exchange, self.RoutingKey);
            }

            return cloudEvent;
        }
    }
}
