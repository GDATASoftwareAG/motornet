using System;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Hosting.RabbitMQ.Options;
using Xunit;

namespace Motor.Extensions.Hosting.RabbitMQ_UnitTest
{
    public class RabbitMQConnectionFactoryTests
    {
        [Fact]
        public void FromConsumerConfig_NullConfig_Throws()
        {
            var factory = new RabbitMQConnectionFactory();

            Assert.Throws<ArgumentNullException>(() => factory.From((RabbitMQConsumerOptions<string>)null));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("  ")]
        [InlineData(null)]
        public void FromConsumerConfig_NullOrEmptyHost_Throws(string host)
        {
            var factory = new RabbitMQConnectionFactory();
            var cfg = GetConsumerConfig(host, "user", "password", "vHost", "name", "exchange", "routingKey");

            Assert.Throws<ArgumentException>(() => factory.From(cfg));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("  ")]
        [InlineData(null)]
        public void FromConsumerConfig_NullOrEmptyUser_Throws(string user)
        {
            var factory = new RabbitMQConnectionFactory();
            var cfg = GetConsumerConfig("host", user, "password", "vHost", "name", "exchange", "routingKey");

            Assert.Throws<ArgumentException>(() => factory.From(cfg));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("  ")]
        [InlineData(null)]
        public void FromConsumerConfig_NullOrEmptyPassword_Throws(string password)
        {
            var factory = new RabbitMQConnectionFactory();
            var cfg = GetConsumerConfig("host", "user", password, "vHost", "name", "exchange", "routingKey");

            Assert.Throws<ArgumentException>(() => factory.From(cfg));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("  ")]
        [InlineData(null)]
        public void FromConsumerConfig_NullOrEmptyVirtualHost_Throws(string vHost)
        {
            var factory = new RabbitMQConnectionFactory();
            var cfg = GetConsumerConfig("host", "user", "password", vHost, "name", "exchange", "routingKey");

            Assert.Throws<ArgumentException>(() => factory.From(cfg));
        }

        [Fact]
        public void FromConsumerConfig_NoQueueConfig_Throws()
        {
            var factory = new RabbitMQConnectionFactory();
            var cfg = new RabbitMQConsumerOptions<string>
            {
                Host = "host",
                VirtualHost = "vHost",
                User = "user",
                Password = "password",
                Queue = null
            };

            Assert.Throws<ArgumentException>(() => factory.From(cfg));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("  ")]
        [InlineData(null)]
        public void FromConsumerConfig_NullOrEmptyQueueName_Throws(string name)
        {
            var factory = new RabbitMQConnectionFactory();
            var cfg = GetConsumerConfig("host", "user", "password", "vHost", name, "exchange", "routingKey");

            Assert.Throws<ArgumentException>(() => factory.From(cfg));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("  ")]
        [InlineData(null)]
        public void FromConsumerConfig_NullOrEmptyExchange_Throws(string exchange)
        {
            var factory = new RabbitMQConnectionFactory();
            var cfg = GetConsumerConfig("host", "user", "password", "vHost", "qName", exchange, "routingKey");

            Assert.Throws<ArgumentException>(() => factory.From(cfg));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("  ")]
        [InlineData(null)]
        public void FromConsumerConfig_NullOrEmptyRoutingKey_Throws(string routingKey)
        {
            var factory = new RabbitMQConnectionFactory();
            var cfg = GetConsumerConfig("host", "user", "password", "vHost", "qName", "exchange", routingKey);

            Assert.Throws<ArgumentException>(() => factory.From(cfg));
        }

        [Fact]
        public void FromConsumerConfig_CorrectConfig_ContainsAllValues()
        {
            const string host = "host";
            const string user = "User";
            const string password = "password";
            const string vHost = "vHost";
            const int port = 1000;
            var factory = new RabbitMQConnectionFactory();
            var cfg = GetConsumerConfig(host, user, password, vHost, "test", "exchange", "test");
            cfg.Port = port;

            var connectionFactory = factory.From(cfg);

            Assert.Equal(user, connectionFactory.UserName);
            Assert.Equal(password, connectionFactory.Password);
            Assert.Equal(vHost, connectionFactory.VirtualHost);
        }

        [Fact]
        public void FromPublisherConfig_NullConfig_Throws()
        {
            var factory = new RabbitMQConnectionFactory();

            Assert.Throws<ArgumentNullException>(() => factory.From((RabbitMQPublisherOptions<string>)null));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("  ")]
        [InlineData(null)]
        public void FromPublisherConfig_NullOrEmptyHost_Throws(string host)
        {
            var factory = new RabbitMQConnectionFactory();
            var cfg = GetPublisherConfig(host, "user", "password", "vHost", "exchange", "routingKey");

            Assert.Throws<ArgumentException>(() => factory.From(cfg));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("  ")]
        [InlineData(null)]
        public void FromPublisherConfig_NullOrEmptyUser_Throws(string user)
        {
            var factory = new RabbitMQConnectionFactory();
            var cfg = GetPublisherConfig("host", user, "password", "vHost", "exchange", "routingKey");

            Assert.Throws<ArgumentException>(() => factory.From(cfg));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("  ")]
        [InlineData(null)]
        public void FromPublisherConfig_NullOrEmptyPassword_Throws(string password)
        {
            var factory = new RabbitMQConnectionFactory();
            var cfg = GetPublisherConfig("host", "user", password, "vHost", "exchange", "routingKey");

            Assert.Throws<ArgumentException>(() => factory.From(cfg));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("  ")]
        [InlineData(null)]
        public void FromPublisherConfig_NullOrEmptyVirtualHost_Throws(string vHost)
        {
            var factory = new RabbitMQConnectionFactory();
            var cfg = GetPublisherConfig("host", "user", "password", vHost, "exchange", "routingKey");

            Assert.Throws<ArgumentException>(() => factory.From(cfg));
        }

        [Fact]
        public void FromPublisherConfig_NullPublishingTarget_Throws()
        {
            var factory = new RabbitMQConnectionFactory();
            var cfg = new RabbitMQPublisherOptions<string>
            {
                Host = "host",
                User = "user",
                Password = "password",
                VirtualHost = "vHost",
                PublishingTarget = null
            };

            Assert.Throws<ArgumentException>(() => factory.From(cfg));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("  ")]
        [InlineData(null)]
        public void FromPublisherConfig_NullOrEmptyExchange_Throws(string exchange)
        {
            var factory = new RabbitMQConnectionFactory();
            var cfg = GetPublisherConfig("host", "user", "password", "vHost", exchange, "routingKey");

            Assert.Throws<ArgumentException>(() => factory.From(cfg));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("  ")]
        [InlineData(null)]
        public void FromPublisherConfig_NullOrEmptyRoutingKey_Throws(string routingKey)
        {
            var factory = new RabbitMQConnectionFactory();
            var cfg = GetPublisherConfig("host", "user", "password", "vHost", "exchange", routingKey);

            Assert.Throws<ArgumentException>(() => factory.From(cfg));
        }

        [Fact]
        public void FromPublisherConfig_CorrectConfig_ContainsAllValues()
        {
            const string host = "host";
            const string user = "User";
            const string password = "password";
            const string vHost = "vHost";
            const int port = 1000;
            var factory = new RabbitMQConnectionFactory();
            var cfg = GetPublisherConfig(host, user, password, vHost, "exchange", "test");
            cfg.Port = port;

            var connectionFactory = factory.From(cfg);

            Assert.Equal(user, connectionFactory.UserName);
            Assert.Equal(password, connectionFactory.Password);
            Assert.Equal(vHost, connectionFactory.VirtualHost);
        }

        private RabbitMQConsumerOptions<string> GetConsumerConfig(string host, string user, string password,
            string virtualHost,
            string name, string exchange, string routingKey)
        {
            return new RabbitMQConsumerOptions<string>
            {
                Host = host,
                VirtualHost = virtualHost,
                User = user,
                Password = password,
                Queue = new RabbitMQQueueOptions
                {
                    Name = name,
                    Bindings = new[]
                    {
                        new RabbitMQBindingOptions
                        {
                            Exchange = exchange,
                            RoutingKey = routingKey
                        }
                    }
                }
            };
        }

        private RabbitMQPublisherOptions<string> GetPublisherConfig(string host, string user, string password,
            string virtualHost, string exchange, string routingKey)
        {
            return new RabbitMQPublisherOptions<string>
            {
                Host = host,
                Password = password,
                User = user,
                VirtualHost = virtualHost,
                PublishingTarget = new RabbitMQBindingOptions
                {
                    Exchange = exchange,
                    RoutingKey = routingKey
                }
            };
        }
    }
}
