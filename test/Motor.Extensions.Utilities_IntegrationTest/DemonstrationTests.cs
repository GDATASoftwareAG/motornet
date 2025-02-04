using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Motor.Extensions.Diagnostics.Metrics.Abstractions;
using Motor.Extensions.Diagnostics.Telemetry;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.Hosting.Publisher;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Hosting.RabbitMQ_IntegrationTest;
using Motor.Extensions.Utilities;
using Prometheus.Client;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace Motor.Extensions.Utilities_IntegrationTest;

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
        using var host = GetReverseStringService();
        var channel = await (await Fixture.ConnectionAsync()).CreateChannelAsync();
        await CreateQueueForServicePublisherWithPublisherBindingFromConfigAsync(channel);

        await host.StartAsync();
        PublishMessageIntoQueueOfServiceAsync(channel, message);

        var actual = await GetMessageFromDestinationQueue(channel);
        Assert.Equal("54321", actual);
        await host.StopAsync();
    }

    private static IHost GetReverseStringService()
    {
        var host = MotorHost.CreateDefaultBuilder()
            .ConfigureSingleOutputService<string, string>()
            .ConfigureServices((_, services) =>
            {
                services.AddTransient<ISingleOutputService<string, string>, ReverseStringConverter>();
            })
            .ConfigureConsumer<string>((_, builder) =>
            {
                builder.AddRabbitMQ();
                builder.AddDeserializer<StringDeserializer>();
            })
            .ConfigurePublisher<string>((_, builder) =>
            {
                builder.AddRabbitMQ();
                builder.AddSerializer<StringSerializer>();
            })
            .Build();

        return host;
    }

    private static async Task<string> GetMessageFromDestinationQueue(IChannel channel)
    {
        var taskCompletionSource = new TaskCompletionSource();
        var destinationQueueName = Environment.GetEnvironmentVariable("DestinationQueueName") ?? "DefaultQueueName";
        var consumer = new AsyncEventingBasicConsumer(channel);
        var messageFromDestinationQueue = string.Empty;
        consumer.ReceivedAsync += (_, args) =>
        {
            var bytes = args.Body;
            messageFromDestinationQueue = Encoding.UTF8.GetString(bytes.ToArray());
            taskCompletionSource.TrySetResult();
            return Task.CompletedTask;
        };
        await channel.BasicConsumeAsync(destinationQueueName, false, consumer);
        await Task.WhenAny(taskCompletionSource.Task, Task.Delay(TimeSpan.FromSeconds(10)));

        return messageFromDestinationQueue;
    }

    protected class ReverseStringConverter : ISingleOutputService<string, string>
    {
        private readonly ILogger<ReverseStringConverter> _logger;
        private readonly IMetricFamily<ISummary> _summary;

        public ReverseStringConverter(ILogger<ReverseStringConverter> logger,
            IMetricsFactory<ReverseStringConverter> metricsFactory)
        {
            _logger = logger;
            _summary = metricsFactory.CreateSummary("summaryName", "summaryHelpString", new[] { "someLabel" });
        }

        public Task<MotorCloudEvent<string>?> ConvertMessageAsync(MotorCloudEvent<string> dataCloudEvent,
            CancellationToken token = default)
        {
            var parentContext = dataCloudEvent.GetActivityContext();
            Assert.NotEqual(default, parentContext);
            _logger.LogInformation("log your request");
            var tmpChar = dataCloudEvent.TypedData.ToCharArray();
            var reversed = tmpChar.Reverse().ToArray();
            _summary.WithLabels("collect_your_metrics").Observe(1.0);
            return Task.FromResult<MotorCloudEvent<string>?>(dataCloudEvent.CreateNew(new string(reversed)));
        }
    }
}
