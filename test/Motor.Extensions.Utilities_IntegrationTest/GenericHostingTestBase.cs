using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Motor.Extensions.Conversion.Abstractions;
using Motor.Extensions.Hosting.RabbitMQ_IntegrationTest;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using RandomExtensions;

namespace Motor.Extensions.Utilities_IntegrationTest
{
    public abstract class GenericHostingTestBase
    {
        protected RabbitMQFixture Fixture { get; }
        private readonly Random _random = new Random();

        protected GenericHostingTestBase(RabbitMQFixture fixture) => Fixture = fixture;

        protected void PrepareQueues(int prefetchCount = 1)
        {
            var consumerQueueName = _random.NextString(10);
            var publisherRoutingKey = _random.NextString(10);
            var destinationQueueName = _random.NextString(10);
            Environment.SetEnvironmentVariable("RabbitMQConsumer__Port", Fixture.Port.ToString());
            Environment.SetEnvironmentVariable("RabbitMQConsumer__Host", Fixture.Hostname);
            Environment.SetEnvironmentVariable("RabbitMQConsumer__Queue__Name", consumerQueueName);
            Environment.SetEnvironmentVariable("RabbitMQConsumer__PrefetchCount", prefetchCount.ToString());
            Environment.SetEnvironmentVariable("RabbitMQPublisher__PublishingTarget__RoutingKey", publisherRoutingKey);
            Environment.SetEnvironmentVariable("RabbitMQPublisher__Port", Fixture.Port.ToString());
            Environment.SetEnvironmentVariable("RabbitMQPublisher__Host", Fixture.Hostname);
            Environment.SetEnvironmentVariable("DestinationQueueName", destinationQueueName);
        }

        protected static async Task CreateQueueForServicePublisherWithPublisherBindingFromConfig(IModel channel)
        {
            var destinationQueueName = Environment.GetEnvironmentVariable("DestinationQueueName");
            const string destinationExchange = "amq.topic";
            var destinationRoutingKey =
                Environment.GetEnvironmentVariable("RabbitMQPublisher__PublishingTarget__RoutingKey");
            var emptyArguments = new Dictionary<string, object>();
            channel.QueueDeclare(destinationQueueName, true, false, false, emptyArguments);
            channel.QueueBind(destinationQueueName, destinationExchange, destinationRoutingKey, emptyArguments);
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        protected static void PublishMessageIntoQueueOfService(IModel channel, string messageToPublish,
            IDictionary<string, object> rabbitMqHeaders = null)
        {
            var basicProperties = channel.CreateBasicProperties();
            
            if (rabbitMqHeaders != null)
            {
                basicProperties.Headers = rabbitMqHeaders;
            }

            channel.BasicPublish("amq.topic", "serviceQueue", true, basicProperties,
                Encoding.UTF8.GetBytes(messageToPublish));
        }
    }

    internal class StringSerializer : IMessageSerializer<string>
    {
        public byte[] Serialize(string message)
        {
            return Encoding.UTF8.GetBytes(message);
        }
    }

    internal class StringDeserializer : IMessageDeserializer<string>
    {
        public string Deserialize(byte[] message)
        {
            return Encoding.UTF8.GetString(message);
        }
    }
}
