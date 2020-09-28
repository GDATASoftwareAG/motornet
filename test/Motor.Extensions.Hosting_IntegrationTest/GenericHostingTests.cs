using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Motor.Extensions.Conversion.Abstractions;
using Motor.Extensions.Diagnostics.Logging;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Diagnostics.Tracing;
using Motor.Extensions.Hosting;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.Hosting.Publisher;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Hosting.RabbitMQ_IntegrationTest;
using Motor.Extensions.Utilities;
using OpenTracing;
using OpenTracing.Mock;
using OpenTracing.Propagation;
using Prometheus.Client.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RandomDataGenerator.FieldOptions;
using RandomDataGenerator.Randomizers;
using Serilog;
using Xunit;

namespace Motor.Extensions.Hosting_IntegrationTest
{
    [Collection("GenericHosting")]
    public class GenericHostingTests : IClassFixture<RabbitMQFixture>
    {
        private readonly RabbitMQFixture _fixture;
        private readonly Random _random = new Random();

        public GenericHostingTests(RabbitMQFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact(Timeout = 20000)]
        public async Task
            StartAsync_SetupAndStartReverseStringServiceAndPublishMessageIntoServiceQueue_MessageInDestinationQueueIsReversed()
        {
            PrepareQueues();

            var message = "12345";
            var host = GetReverseStringService();
            var channel = _fixture.Connection.CreateModel();
            await CreateQueueForServicePublisherWithPublisherBindingFromConfig(channel);

            await host.StartAsync();
            await PublishMessageIntoQueueOfService(channel, message);

            var actual = await GetMessageFromDestinationQueue(channel);
            Assert.Equal("54321", actual);
            await host.StopAsync();
        }

        [Fact(Timeout = 20000)]
        public async Task StartAsync_CreateSpanAsReference_ContextIsReferenced()
        {
            PrepareQueues();

            var tracer = new MockTracer();
            var host = GetReverseStringService(tracer);
            var channel = _fixture.Connection.CreateModel();
            await CreateQueueForServicePublisherWithPublisherBindingFromConfig(channel);

            await host.StartAsync();
            var start = tracer.BuildSpan("OtherService").Start();
            var rabbitMqHeaders = new Dictionary<string, object>();
            tracer.Inject(start.Context, BuiltinFormats.TextMap, new RabbitMQHeadersMap(rabbitMqHeaders));
            start.Finish();
            await PublishMessageIntoQueueOfService(channel, "12345", rabbitMqHeaders);

            await GetMessageFromDestinationQueue(channel);
            Assert.Contains(tracer.FinishedSpans(), t => t.ParentId != null);
            await host.StopAsync();
        }

        [Fact(Timeout = 20000)]
        public async Task StartAsync_CreateSpanWithoutReference_ContextNoReferencedSpan()
        {
            PrepareQueues();

            var tracer = new MockTracer();
            var host = GetReverseStringService(tracer);
            var channel = _fixture.Connection.CreateModel();
            await CreateQueueForServicePublisherWithPublisherBindingFromConfig(channel);

            await host.StartAsync();
            var start = tracer.BuildSpan("OtherService").Start();
            start.Finish();
            await PublishMessageIntoQueueOfService(channel, "12345");

            await GetMessageFromDestinationQueue(channel);
            Assert.Contains(tracer.FinishedSpans(), t => t.ParentId == null);
            await host.StopAsync();
        }

        [Fact(Timeout = 20000)]
        public async Task StartAsync_CreateSpanAndPublished_ContextIsPublished()
        {
            PrepareQueues();

            var host = GetReverseStringService();
            var channel = _fixture.Connection.CreateModel();
            await CreateQueueForServicePublisherWithPublisherBindingFromConfig(channel);

            await host.StartAsync();
            await PublishMessageIntoQueueOfService(channel, "12345");

            var dictionary = await GetHeaderFromDestinationQueue(channel);

            var keyValuePairs = dictionary.ToList();
            Assert.Equal(2, keyValuePairs.Count);
            await host.StopAsync();
        }

        [Fact(Timeout = 20000)]
        public async Task StartAsync_CreateSpan_TraceIsPublished()
        {
            PrepareQueues();

            var tracer = new MockTracer();
            var host = GetReverseStringService(tracer);
            var channel = _fixture.Connection.CreateModel();
            await CreateQueueForServicePublisherWithPublisherBindingFromConfig(channel);

            await host.StartAsync();
            await PublishMessageIntoQueueOfService(channel, "12345");

            await GetMessageFromDestinationQueue(channel);
            var finishedSpans = tracer.FinishedSpans();
            Assert.Single(finishedSpans);
            await host.StopAsync();
        }

        private IHost GetReverseStringService(ITracer tracer = null)
        {
            tracer ??= new MockTracer();

            var host = new MotorHostBuilder(new HostBuilder(), false)
                .UseSetting(MotorHostDefaults.EnablePrometheusEndpointKey, false.ToString())
                .ConfigureSerilog()
                .ConfigurePrometheus()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient(provider =>
                    {
                        var mock = new Mock<IApplicationNameService>();
                        mock.Setup(t => t.GetVersion()).Returns("test");
                        mock.Setup(t => t.GetLibVersion()).Returns("test");
                        mock.Setup(t => t.GetSource()).Returns(new Uri("motor://test"));
                        return mock.Object;
                    });
                    services.AddTransient<IMessageConverter<string, string>, ReverseStringConverter>();
                    services.AddTransient<IMessageSerializer<string>, StringSerializer>();
                    services.AddTransient<IMessageDeserializer<string>, StringDeserializer>();
                    services.AddTransient<IMessageHandler<string>, MessageHandler<string, string>>();
                    services.AddTransient<DelegatingMessageHandler<string>, TracingDelegatingMessageHandler<string>>();
                    services.AddQueuedGenericService<string>();
                    services.AddSingleton(provider => tracer);
                    services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
                })
                .ConfigureConsumer<string>((context, builder) =>
                {
                    builder.AddRabbitMQ();
                    builder.AddDeserializer<StringDeserializer>();
                })
                .ConfigurePublisher<string>((context, builder) =>
                {
                    builder.AddRabbitMQ();
                    builder.AddSerializer<StringSerializer>();
                })
                .ConfigureAppConfiguration((builder, config) =>
                {
                    config.AddJsonFile("appsettings.json", true, false);
                    config.AddEnvironmentVariables();
                })
                .Build();

            return host;
        }

        private void PrepareQueues()
        {
            var randomizerString = RandomizerFactory.GetRandomizer(new FieldOptionsTextRegex {Pattern = @"^[A-Z]{10}"});
            Environment.SetEnvironmentVariable("RabbitMQConsumer__Port", _fixture.Port.ToString());
            Environment.SetEnvironmentVariable("RabbitMQConsumer__Host", _fixture.Hostname);
            Environment.SetEnvironmentVariable("RabbitMQPublisher__Port", _fixture.Port.ToString());
            Environment.SetEnvironmentVariable("RabbitMQPublisher__Host", _fixture.Hostname);
            Environment.SetEnvironmentVariable("RabbitMQConsumer__Queue__Name", randomizerString.Generate());
            Environment.SetEnvironmentVariable("RabbitMQPublisher__PublishingTarget__RoutingKey",
                randomizerString.Generate());
            Environment.SetEnvironmentVariable("DestinationQueueName", randomizerString.Generate());
        }

        private async Task CreateQueueForServicePublisherWithPublisherBindingFromConfig(IModel channel)
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

        private async Task PublishMessageIntoQueueOfService(IModel channel, string messageToPublish,
            IDictionary<string, object> rabbitMqHeaders = null)
        {
            var basicProperties = channel.CreateBasicProperties();

            if (rabbitMqHeaders != null) basicProperties.Headers = rabbitMqHeaders;

            channel.BasicPublish("amq.topic", "serviceQueue", true, basicProperties,
                Encoding.UTF8.GetBytes(messageToPublish));

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        private async Task<string> GetMessageFromDestinationQueue(IModel channel)
        {
            var destinationQueueName = Environment.GetEnvironmentVariable("DestinationQueueName");
            var consumer = new EventingBasicConsumer(channel);
            var messageFromDestinationQueue = string.Empty;
            consumer.Received += (sender, args) =>
            {
                var bytes = args.Body;
                messageFromDestinationQueue = Encoding.UTF8.GetString(bytes.ToArray());
            };
            channel.BasicConsume(destinationQueueName, false, consumer);
            while (messageFromDestinationQueue == string.Empty) await Task.Delay(TimeSpan.FromMilliseconds(50));

            return messageFromDestinationQueue;
        }

        private async Task<ITextMap> GetHeaderFromDestinationQueue(IModel channel)
        {
            var destinationQueueName = Environment.GetEnvironmentVariable("DestinationQueueName");
            var consumer = new EventingBasicConsumer(channel);
            IDictionary<string, object> headerFromMessageInDestinationQueue = null;
            consumer.Received += (sender, args) =>
            {
                headerFromMessageInDestinationQueue = args.BasicProperties.Headers;
            };
            channel.BasicConsume(destinationQueueName, false, consumer);
            while (headerFromMessageInDestinationQueue == null) await Task.Delay(TimeSpan.FromMilliseconds(50));

            return new RabbitMQHeadersMap(headerFromMessageInDestinationQueue);
            ;
        }

        protected class ReverseStringConverter : IMessageConverter<string, string>
        {
            private readonly ILogger<ReverseStringConverter> _logger;
            private readonly IMetricFamily<ISummary> _summary;

            public ReverseStringConverter(ILogger<ReverseStringConverter> logger,
                IMetricsFactory<ReverseStringConverter> metricsFactory)
            {
                _logger = logger;
                _summary = metricsFactory.CreateSummary("summaryName", "summaryHelpString", "someLabel");
            }

            public Task<MotorCloudEvent<string>> ConvertMessageAsync(MotorCloudEvent<string> dataCloudEvent,
                CancellationToken token = default)
            {
                _logger.LogInformation("log your request");
                var tmpChar = dataCloudEvent.TypedData.ToCharArray();
                var reversed = tmpChar.Reverse().ToArray();
                _summary.WithLabels("collect_your_metrics").Observe(1.0);
                return Task.FromResult(dataCloudEvent.CreateNew(new string(reversed)));
            }
        }

        protected class StringSerializer : IMessageSerializer<string>
        {
            public byte[] Serialize(string message)
            {
                return Encoding.UTF8.GetBytes(message);
            }
        }

        protected class StringDeserializer : IMessageDeserializer<string>
        {
            public string Deserialize(byte[] message)
            {
                return Encoding.UTF8.GetString(message);
            }
        }
    }
}
