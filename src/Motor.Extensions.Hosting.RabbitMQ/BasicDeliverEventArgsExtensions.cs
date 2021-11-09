using System;
using Motor.Extensions.ContentEncoding.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Motor.Extensions.Hosting.RabbitMQ;

public static class BasicDeliverEventArgsExtensions
{
    public static MotorCloudEvent<byte[]> ExtractCloudEvent(this BasicDeliverEventArgs self,
        IApplicationNameService applicationNameService, ReadOnlyMemory<byte> body, bool extractBindingKey)
    {
        var cloudEvent = self.BasicProperties.ExtractCloudEvent(applicationNameService, body);

        if (extractBindingKey)
        {
            cloudEvent.SetRabbitMQBinding(self.Exchange, self.RoutingKey);
        }

        return cloudEvent;
    }

}
