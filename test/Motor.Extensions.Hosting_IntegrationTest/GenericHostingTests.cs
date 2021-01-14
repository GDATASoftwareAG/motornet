using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Extensions;
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
using Motor.Extensions.Hosting.RabbitMQ.Options;
using Motor.Extensions.TestUtilities;
using Motor.Extensions.Utilities;
using Prometheus.Client;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RandomDataGenerator.FieldOptions;
using RandomDataGenerator.Randomizers;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Motor.Extensions.Hosting_IntegrationTest
{
    [Collection("GenericHosting")]
    public class GenericHostingTests : IClassFixture<RabbitMQFixture>
    {
        private readonly RabbitMQFixture _fixture;
        private readonly ITestOutputHelper _outputHelper;

        private readonly ActivityListener _listener;

        public GenericHostingTests(RabbitMQFixture fixture, ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == OpenTelemetryOptions.DefaultActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
                    ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = _ => { _outputHelper.WriteLine("test"); },
            };
            ActivitySource.AddActivityListener(_listener);
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

            var host = GetReverseStringService();
            var channel = _fixture.Connection.CreateModel();
            await CreateQueueForServicePublisherWithPublisherBindingFromConfig(channel);

            await host.StartAsync();

            var extensions = new List<ICloudEventExtension>();
            var randomActivity = CreateRandomActivity();
            var distributedTracingExtension = new DistributedTracingExtension();
            distributedTracingExtension.SetActivity(randomActivity);
            extensions.Add(distributedTracingExtension);
            var motorCloudEvent = MotorCloudEvent.CreateTestCloudEvent(new byte[0], extensions: extensions);

            await PublishMessageIntoQueueOfService(channel, "12345", motorCloudEvent);

            var ctx = await GetActivityContextFromDestinationQueue(channel);

            Assert.Equal(randomActivity.Context.TraceId, ctx.TraceId);
            Assert.NotEqual(randomActivity.Context.SpanId, ctx.SpanId);
            await host.StopAsync();
        }

        [Fact(Timeout = 20000)]
        public async Task StartAsync_CreateSpanWithoutReference_ContextNoReferencedSpan()
        {
            PrepareQueues();

            //var tracer = new MockTracer();
            var host = GetReverseStringService();
            var channel = _fixture.Connection.CreateModel();
            await CreateQueueForServicePublisherWithPublisherBindingFromConfig(channel);

            await host.StartAsync();
            //var start = tracer.BuildSpan("OtherService").Start();
            //start.Finish();
            await PublishMessageIntoQueueOfService(channel, "12345");

            await GetMessageFromDestinationQueue(channel);
            //Assert.Contains(tracer.FinishedSpans(), t => t.ParentId is null);
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

            var dictionary = await GetHeadersFromDestinationQueue(channel);

            var keyValuePairs = dictionary.ToList();
            Assert.True(keyValuePairs.Count > 0);
            await host.StopAsync();
        }

        [Fact(Timeout = 20000)]
        public async Task StartAsync_CreateSpan_TraceIsPublished()
        {
            PrepareQueues();
            var traceIsPublished = false;
            _listener.ActivityStarted = activity => { traceIsPublished = true; };

            var host = GetReverseStringService();
            var channel = _fixture.Connection.CreateModel();
            await CreateQueueForServicePublisherWithPublisherBindingFromConfig(channel);

            await host.StartAsync();
            await PublishMessageIntoQueueOfService(channel, "12345");

            await GetHeadersFromDestinationQueue(channel);
            Assert.True(traceIsPublished);
            await host.StopAsync();
        }

        public Activity CreateRandomActivity()
        {
            var activity = new Activity(nameof(CreateRandomActivity));
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.Start();
            return activity;
        }

        private IHost GetReverseStringService()
        {
            var host = new MotorHostBuilder(new HostBuilder(), false)
                .UseSetting(MotorHostDefaults.EnablePrometheusEndpointKey, false.ToString())
                .ConfigureSerilog()
                .ConfigurePrometheus()
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient(provider =>
                    {
                        var mock = new Mock<IApplicationNameService>();
                        mock.Setup(t => t.GetVersion()).Returns("test");
                        mock.Setup(t => t.GetLibVersion()).Returns("test");
                        mock.Setup(t => t.GetSource()).Returns(new Uri("motor://test"));
                        return mock.Object;
                    });
                    services.AddTransient<ISingleOutputService<string, string>, ReverseStringConverter>();
                    services.AddTransient<IMessageSerializer<string>, StringSerializer>();
                    services.AddTransient<IMessageDeserializer<string>, StringDeserializer>();
                    services.AddTransient<INoOutputService<string>, SingleOutputServiceAdapter<string, string>>();
                    services.AddTransient<DelegatingMessageHandler<string>, TracingDelegatingMessageHandler<string>>();
                    services.AddQueuedGenericService<string>();
                    //services.AddSingleton(provider => tracer);
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
            var randomizerString = RandomizerFactory.GetRandomizer(new FieldOptionsTextRegex { Pattern = @"^[A-Z]{10}" });
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
            MotorCloudEvent<byte[]> cloudEvent = null)
        {
            var basicProperties = channel.CreateBasicProperties();
            if (cloudEvent is not null)
            {
                basicProperties.Update(cloudEvent, new RabbitMQPublisherOptions<string>(), new JsonEventFormatter());
            }

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

        private async Task<IDictionary<string, object>> GetHeadersFromDestinationQueue(IModel channel)
        {
            var destinationQueueName = Environment.GetEnvironmentVariable("DestinationQueueName");
            var consumer = new EventingBasicConsumer(channel);
            IDictionary<string, object> headerFromMessageInDestinationQueue = null;
            consumer.Received += (sender, args) =>
            {
                headerFromMessageInDestinationQueue = args.BasicProperties.Headers;
            };
            channel.BasicConsume(destinationQueueName, false, consumer);
            while (headerFromMessageInDestinationQueue is null) await Task.Delay(TimeSpan.FromMilliseconds(50));

            return headerFromMessageInDestinationQueue;
        }

        private async Task<ActivityContext> GetActivityContextFromDestinationQueue(IModel channel)
        {
            var headers = await GetHeadersFromDestinationQueue(channel);
            var traceparent = Encoding.UTF8
                .GetString((byte[])headers[
                    $"{BasicPropertiesExtensions.CloudEventPrefix}{DistributedTracingExtension.TraceParentAttributeName}"])
                .Trim('"');
            return ActivityContext.Parse(traceparent, null);
        }

        protected class ReverseStringConverter : ISingleOutputService<string, string>
        {
            private readonly ILogger<ReverseStringConverter> _logger;
            private readonly IMetricFamily<ISummary> _summary;
            private static ActivitySource ActivitySource = new(OpenTelemetryOptions.DefaultActivitySourceName);

            public ReverseStringConverter(ILogger<ReverseStringConverter> logger,
                IMetricsFactory<ReverseStringConverter> metricsFactory)
            {
                _logger = logger;
                _summary = metricsFactory.CreateSummary("summaryName", "summaryHelpString", new[] { "someLabel" });
            }

            public Task<MotorCloudEvent<string>> ConvertMessageAsync(MotorCloudEvent<string> dataCloudEvent,
                CancellationToken token = default)
            {
                _logger.LogInformation("log your request");
                var tmpChar = dataCloudEvent.TypedData.ToCharArray();
                if (!ActivitySource.HasListeners())
                {
                    throw new ArgumentException();
                }
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
