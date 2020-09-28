using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Hosting.Publisher;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Hosting.RabbitMQ_IntegrationTest;
using Motor.Extensions.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus.Client;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;
using Prometheus.Client.Abstractions;

namespace Motor.Extensions.Utilities_IntegrationTest
{
    [Collection("GenericHosting")]
    public class DemonstrationTests : GenericHostingTestBase, IClassFixture<RabbitMQFixture>
    {
        public DemonstrationTests(RabbitMQFixture fixture)
            : base(fixture)
        {
        }

        [Fact(Timeout = 60000)]
        public async Task
            StartAsync_SetupAndStartReverseStringServiceAndPublishMessageIntoServiceQueue_MessageInDestinationQueueIsReversed()
        {
            PrepareQueues();

            const string message = "12345";
            var host = GetReverseStringService();
            var channel = Fixture.Connection.CreateModel();
            await CreateQueueForServicePublisherWithPublisherBindingFromConfig(channel);

            await host.StartAsync();
            PublishMessageIntoQueueOfService(channel, message);

            var actual = await GetMessageFromDestinationQueue(channel);
            Assert.Equal("54321", actual);
            await host.StopAsync();
        }

        private static IHost GetReverseStringService()
        {
            var host = MotorHost.CreateDefaultBuilder()
                .ConfigureDefaultMessageHandler<string, string>()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<IMessageConverter<string, string>, ReverseStringConverter>();
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
                .Build();

            return host;
        }

        private static async Task<string> GetMessageFromDestinationQueue(IModel channel)
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
            while (messageFromDestinationQueue == string.Empty)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50));
            }

            return messageFromDestinationQueue;
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


            public Task<MotorCloudEvent<string>> ConvertMessageAsync(MotorCloudEvent<string> dataCloudEvent, CancellationToken token = default)
            {
                _logger.LogInformation("log your request");
                var tmpChar = dataCloudEvent.TypedData.ToCharArray();
                var reversed = tmpChar.Reverse().ToArray();
                _summary.WithLabels("collect_your_metrics").Observe(1.0);
                return Task.FromResult(dataCloudEvent.CreateNew(new string(reversed)));
            }

            public Task<MotorCloudEvent<string>> ConvertMessageAsContainerAsync(MotorCloudEvent<string> dataCloudEvent, CancellationToken token = default)
            {
                throw new NotImplementedException();
            }
        }
    }
}
