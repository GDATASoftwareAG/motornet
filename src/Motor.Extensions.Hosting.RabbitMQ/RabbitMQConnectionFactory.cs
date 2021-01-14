using System;
using System.Linq;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using RabbitMQ.Client;

namespace Motor.Extensions.Hosting.RabbitMQ
{
    public interface IRabbitMQConnectionFactory
    {
        IConnectionFactory From<T>(RabbitMQConsumerOptions<T> consumerOptions);
        IConnectionFactory From<T>(RabbitMQPublisherOptions<T> publisherOptions);
    }

    public class RabbitMQConnectionFactory : IRabbitMQConnectionFactory
    {
        public IConnectionFactory From<T>(RabbitMQConsumerOptions<T> consumerOptions)
        {
            ThrowIfConfigInvalid(consumerOptions);
            return FromConfig(consumerOptions);
        }

        public IConnectionFactory From<T>(RabbitMQPublisherOptions<T> publisherOptions)
        {
            ThrowIfConfigInvalid(publisherOptions);
            return FromConfig(publisherOptions);
        }

        private void ThrowIfConfigInvalid<T>(RabbitMQConsumerOptions<T> options)
        {
            ThrowIfInvalid(options);
            ThrowIfInvalid(options.Queue);
        }

        private void ThrowIfConfigInvalid<T>(RabbitMQPublisherOptions<T> options)
        {
            ThrowIfInvalid(options);
            ThrowIfInvalid(options.PublishingTarget);
        }

        private void ThrowIfInvalid(RabbitMQBaseOptions baseOptions)
        {
            if (baseOptions is null)
                throw new ArgumentNullException(nameof(baseOptions));
            if (string.IsNullOrWhiteSpace(baseOptions.Host))
                throw new ArgumentException(nameof(baseOptions.Host));
            if (string.IsNullOrWhiteSpace(baseOptions.User))
                throw new ArgumentException(nameof(baseOptions.User));
            if (string.IsNullOrWhiteSpace(baseOptions.Password))
                throw new ArgumentException(nameof(baseOptions.Password));
            if (string.IsNullOrWhiteSpace(baseOptions.VirtualHost))
                throw new ArgumentException(nameof(baseOptions.VirtualHost));
        }

        private void ThrowIfInvalid(RabbitMQQueueOptions queueOptions)
        {
            if (queueOptions is null)
                throw new ArgumentException(nameof(queueOptions));
            if (string.IsNullOrWhiteSpace(queueOptions.Name))
                throw new ArgumentException(nameof(queueOptions.Name));
            if (queueOptions.Bindings is null)
                return;
            if (!queueOptions.Bindings.Any())
                return;
            foreach (var binding in queueOptions.Bindings)
            {
                if (string.IsNullOrWhiteSpace(binding.Exchange))
                    throw new ArgumentException(nameof(binding.Exchange));
                if (string.IsNullOrWhiteSpace(binding.RoutingKey))
                    throw new ArgumentException(nameof(binding.RoutingKey));
            }
        }

        private void ThrowIfInvalid(RabbitMQBindingOptions rabbitMqBindingOptions)
        {
            if (rabbitMqBindingOptions is null)
                throw new ArgumentException(nameof(rabbitMqBindingOptions));
            if (string.IsNullOrWhiteSpace(rabbitMqBindingOptions.Exchange))
                throw new ArgumentException(nameof(rabbitMqBindingOptions.Exchange));
            if (string.IsNullOrWhiteSpace(rabbitMqBindingOptions.RoutingKey))
                throw new ArgumentException(nameof(rabbitMqBindingOptions.RoutingKey));
        }

        private static IConnectionFactory FromConfig(RabbitMQBaseOptions baseOptions)
        {
            return new ConnectionFactory
            {
                HostName = baseOptions.Host,
                Port = baseOptions.Port,
                VirtualHost = baseOptions.VirtualHost,
                UserName = baseOptions.User,
                Password = baseOptions.Password,
                RequestedHeartbeat = baseOptions.RequestedHeartbeat
            };
        }
    }
}
