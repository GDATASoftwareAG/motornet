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
using Motor.Extensions.Diagnostics.Telemetry;
using Motor.Extensions.Hosting;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.Hosting.Publisher;
using Motor.Extensions.TestUtilities;
using Motor.Extensions.Utilities;
using Prometheus.Client;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Motor.Extensions.Hosting_IntegrationTest
{
    public class GenericHostingTests
    {
        private readonly ActivityListener _listener;

        public GenericHostingTests(ITestOutputHelper outputHelper)
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == OpenTelemetryOptions.DefaultActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                    ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = _ => { outputHelper.WriteLine("test"); },
            };
            ActivitySource.AddActivityListener(_listener);
        }

        [Fact(Timeout = 20000)]
        public async Task
            StartAsync_SetupAndStartReverseStringServiceAndPublishMessageIntoServiceQueue_MessageInDestinationQueueIsReversed()
        {
            var consumer = new InMemoryConsumer<string>();
            var publisher = new InMemoryPublisher<string>();
            var host = GetReverseStringService(consumer, publisher);

            var message = "12345";

            await host.StartAsync();
            await ConsumeMessage(consumer, message);

            var actual = Encoding.UTF8.GetString(publisher.Events.First().TypedData);
            Assert.Equal("54321", actual);
            await host.StopAsync();
        }

        [Fact(Timeout = 20000)]
        public async Task StartAsync_CreateSpanAsReference_ContextIsReferenced()
        {
            var consumer = new InMemoryConsumer<string>();
            var publisher = new InMemoryPublisher<string>();
            var host = GetReverseStringService(consumer, publisher);

            await host.StartAsync();

            var extensions = new List<ICloudEventExtension>();
            var randomActivity = CreateRandomActivity();
            var distributedTracingExtension = new DistributedTracingExtension();
            distributedTracingExtension.SetActivity(randomActivity);
            extensions.Add(distributedTracingExtension);
            var motorCloudEvent = MotorCloudEvent.CreateTestCloudEvent(new byte[0], extensions: extensions);
            await consumer.AddMessage(motorCloudEvent);

            var ctx = publisher.Events.First().Extension<DistributedTracingExtension>().GetActivityContext();

            Assert.Equal(randomActivity.Context.TraceId, ctx.TraceId);
            Assert.NotEqual(randomActivity.Context.SpanId, ctx.SpanId);
            await host.StopAsync();
        }

        [Fact(Timeout = 20000)]
        public async Task StartAsync_CreateSpanWithoutReference_NewSpanIsCreated()
        {
            var consumer = new InMemoryConsumer<string>();
            var publisher = new InMemoryPublisher<string>();
            var host = GetReverseStringService(consumer, publisher);
            await host.StartAsync();
            await ConsumeMessage(consumer, "test");

            Assert.Single(publisher.Events);
            var ctx = publisher.Events.First().Extension<DistributedTracingExtension>();

            Assert.NotNull(ctx.TraceParent);
            await host.StopAsync();
        }

        [Fact(Timeout = 20000)]
        public async Task StartAsync_CreateSpan_ActivityListenerGotTrace()
        {
            var consumer = new InMemoryConsumer<string>();
            var publisher = new InMemoryPublisher<string>();
            var traceIsPublished = false;
            _listener.ActivityStarted = _ => { traceIsPublished = true; };

            var host = GetReverseStringService(consumer, publisher);

            await host.StartAsync();
            await ConsumeMessage(consumer, "12345");

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

        private IHost GetReverseStringService(InMemoryConsumer<string> consumer, InMemoryPublisher<string> publisher)
        {
            var host = new MotorHostBuilder(new HostBuilder(), false)
                .UseSetting(MotorHostDefaults.EnablePrometheusEndpointKey, false.ToString())
                .ConfigureSerilog()
                .ConfigurePrometheus()
                .ConfigureServices((_, services) =>
                {
                    services.AddTransient(_ =>
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
                    services.AddTransient<DelegatingMessageHandler<string>, TelemetryDelegatingMessageHandler<string>>();
                    services.AddQueuedGenericService<string>();
                    //services.AddSingleton(provider => tracer);
                    services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
                })
                .ConfigureConsumer<string>((_, builder) =>
                {
                    builder.AddInMemory(consumer);
                    builder.AddDeserializer<StringDeserializer>();
                })
                .ConfigurePublisher<string>((_, builder) =>
                {
                    builder.AddInMemory(publisher);
                    builder.AddSerializer<StringSerializer>();
                })
                .ConfigureAppConfiguration((_, config) =>
                {
                    config.AddJsonFile("appsettings.json", true, false);
                    config.AddEnvironmentVariables();
                })
                .Build();

            return host;
        }

        private Task<ProcessedMessageStatus> ConsumeMessage(InMemoryConsumer<string> consumer,
            string messageToPublish)
        {
            return consumer.AddMessage(MotorCloudEvent.CreateTestCloudEvent(Encoding.UTF8.GetBytes(messageToPublish)));
        }

        protected class ReverseStringConverter : ISingleOutputService<string, string>
        {
            private readonly ILogger<ReverseStringConverter> _logger;
            private readonly IMetricFamily<ISummary> _summary;
            private static readonly ActivitySource ActivitySource = new(OpenTelemetryOptions.DefaultActivitySourceName);

            public ReverseStringConverter(ILogger<ReverseStringConverter> logger,
                IMetricsFactory<ReverseStringConverter> metricsFactory)
            {
                _logger = logger;
                _summary = metricsFactory.CreateSummary("summaryName", "summaryHelpString", new[] { "someLabel" });
            }

            public Task<MotorCloudEvent<string>?> ConvertMessageAsync(MotorCloudEvent<string> dataCloudEvent,
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
                return Task.FromResult<MotorCloudEvent<string>?>(dataCloudEvent.CreateNew(new string(reversed)));
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
