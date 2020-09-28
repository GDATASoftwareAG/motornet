using System;
using System.Linq;
using Motor.Extensions.Hosting.RabbitMQ.Config;
using RabbitMQ.Client;

namespace Motor.Extensions.Hosting.RabbitMQ
{
    public interface IRabbitMQConnectionFactory
    {
        IConnectionFactory From<T>(RabbitMQConsumerConfig<T> consumerConfig);
        IConnectionFactory From<T>(RabbitMQPublisherConfig<T> publisherConfig);
    }

    public class RabbitMQConnectionFactory : IRabbitMQConnectionFactory
    {
        public IConnectionFactory From<T>(RabbitMQConsumerConfig<T> consumerConfig)
        {
            ThrowIfConfigInvalid(consumerConfig);
            return FromConfig(consumerConfig);
        }

        public IConnectionFactory From<T>(RabbitMQPublisherConfig<T> publisherConfig)
        {
            ThrowIfConfigInvalid(publisherConfig);
            return FromConfig(publisherConfig);
        }

        private void ThrowIfConfigInvalid<T>(RabbitMQConsumerConfig<T> config)
        {
            ThrowIfInvalid(config);
            ThrowIfInvalid(config.Queue);
        }

        private void ThrowIfConfigInvalid<T>(RabbitMQPublisherConfig<T> config)
        {
            ThrowIfInvalid(config);
            ThrowIfInvalid(config.PublishingTarget);
        }

        private void ThrowIfInvalid(RabbitMQConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrWhiteSpace(config.Host))
                throw new ArgumentException(nameof(config.Host));
            if (string.IsNullOrWhiteSpace(config.User))
                throw new ArgumentException(nameof(config.User));
            if (string.IsNullOrWhiteSpace(config.Password))
                throw new ArgumentException(nameof(config.Password));
            if (string.IsNullOrWhiteSpace(config.VirtualHost))
                throw new ArgumentException(nameof(config.VirtualHost));
        }

        private void ThrowIfInvalid(RabbitMQQueueConfig queueConfig)
        {
            if (queueConfig == null)
                throw new ArgumentException(nameof(queueConfig));
            if (string.IsNullOrWhiteSpace(queueConfig.Name))
                throw new ArgumentException(nameof(queueConfig.Name));
            if (queueConfig.Bindings == null)
                return;
            if (!queueConfig.Bindings.Any())
                return;
            foreach (var binding in queueConfig.Bindings)
            {
                if (string.IsNullOrWhiteSpace(binding.Exchange))
                    throw new ArgumentException(nameof(binding.Exchange));
                if (string.IsNullOrWhiteSpace(binding.RoutingKey))
                    throw new ArgumentException(nameof(binding.RoutingKey));
            }
        }

        private void ThrowIfInvalid(RabbitMQBindingConfig rabbitMqBindingConfig)
        {
            if (rabbitMqBindingConfig == null)
                throw new ArgumentException(nameof(rabbitMqBindingConfig));
            if (string.IsNullOrWhiteSpace(rabbitMqBindingConfig.Exchange))
                throw new ArgumentException(nameof(rabbitMqBindingConfig.Exchange));
            if (string.IsNullOrWhiteSpace(rabbitMqBindingConfig.RoutingKey))
                throw new ArgumentException(nameof(rabbitMqBindingConfig.RoutingKey));
        }

        private static IConnectionFactory FromConfig(RabbitMQConfig config)
        {
            return new ConnectionFactory
            {
                HostName = config.Host,
                Port = config.Port,
                VirtualHost = config.VirtualHost,
                UserName = config.User,
                Password = config.Password,
                RequestedHeartbeat = config.RequestedHeartbeat
            };
        }
    }
}
