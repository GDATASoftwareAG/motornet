using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
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
public class DemonstrationTests(RabbitMQFixture fixture)
    : GenericHostingTestBase(fixture),
        IClassFixture<RabbitMQFixture>
{
    [Fact]
    public async Task StartAsync_SetupAndStartReverseStringServiceAndPublishMessageIntoServiceQueue_MessageInDestinationQueueIsReversed()
    {
        const string message = "12345";
        using var host = GetReverseStringService(RandomHttpPort);
        await using var connection = await Fixture.ConnectionAsync();
        var publisherChannel = await connection.CreateChannelAsync();
        await CreateQueueForServicePublisherWithPublisherBindingFromConfigAsync(publisherChannel);

        await host.StartAsync();
        await PublishMessageIntoQueueOfServiceAsync(publisherChannel, message);

        await using var consumerChannel = await connection.CreateChannelAsync();

        var actual = await GetMessageFromDestinationQueue(consumerChannel);
        Assert.Equal("54321", actual);
        await host.StopAsync();
    }

    private IHost GetReverseStringService(ushort listenerPort, int prefetchCount = 1)
    {
        var builder = MotorHost
            .CreateDefaultBuilder()
            .ConfigureSingleOutputService<string, string>()
            .ConfigureServices(
                (_, services) =>
                {
                    services.AddTransient<ISingleOutputService<string, string>, ReverseStringConverter>();
                }
            )
            .ConfigureConsumer<string>(
                (_, builder) =>
                {
                    builder.AddRabbitMQ();
                    builder.AddDeserializer<StringDeserializer>();
                }
            )
            .ConfigurePublisher<string>(
                (_, builder) =>
                {
                    builder.AddRabbitMQ();
                    builder.AddSerializer<StringSerializer>();
                }
            )
            .ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        { "RabbitMQConsumer:Port", Fixture.Port.ToString() },
                        { "RabbitMQConsumer:Host", Fixture.Hostname },
                        { "RabbitMQConsumer:Queue:Name", ConsumerQueueName },
                        { "RabbitMQConsumer:PrefetchCount", prefetchCount.ToString() },
                        { "RabbitMQPublisher:PublishingTarget:RoutingKey", RoutingKey },
                        { "RabbitMQPublisher:Port", Fixture.Port.ToString() },
                        { "RabbitMQPublisher:Host", Fixture.Hostname },
                        { "DestinationQueueName", DestinationQueueName },
                    }
                );
            });

        if (builder is MotorHostBuilder motorHostBuilder)
        {
            motorHostBuilder.UseSetting(WebHostDefaults.ServerUrlsKey, $"http://0.0.0.0:{listenerPort}");
        }

        return builder.Build();
    }

    private async Task<string> GetMessageFromDestinationQueue(IChannel channel)
    {
        var taskCompletionSource = new TaskCompletionSource();
        var consumer = new AsyncEventingBasicConsumer(channel);
        var messageFromDestinationQueue = string.Empty;
        consumer.ReceivedAsync += (_, args) =>
        {
            var bytes = args.Body;
            messageFromDestinationQueue = Encoding.UTF8.GetString(bytes.ToArray());
            taskCompletionSource.TrySetResult();
            return Task.CompletedTask;
        };
        await channel.BasicConsumeAsync(DestinationQueueName, false, consumer);
        await Task.WhenAny(taskCompletionSource.Task, Task.Delay(TimeSpan.FromSeconds(60)));

        return messageFromDestinationQueue;
    }

    protected class ReverseStringConverter(
        ILogger<ReverseStringConverter> logger,
        IMetricsFactory<ReverseStringConverter> metricsFactory
    ) : ISingleOutputService<string, string>
    {
        private readonly ILogger<ReverseStringConverter> _logger = logger;
        private readonly IMetricFamily<ISummary> _summary = metricsFactory.CreateSummary(
            "summaryName",
            "summaryHelpString",
            new[] { "someLabel" }
        );

        public Task<MotorCloudEvent<string>?> ConvertMessageAsync(
            MotorCloudEvent<string> dataCloudEvent,
            CancellationToken token = default
        )
        {
            var parentContext = dataCloudEvent.GetActivityContext();
            Assert.NotEqual(default, parentContext);
            _logger.LogInformation("log your request");

            var chars = dataCloudEvent.TypedData.ToCharArray();
            for (var i = 0; i < chars.Length / 2; i++)
            {
                (chars[chars.Length - 1 - i], chars[i]) = (chars[i], chars[chars.Length - 1 - i]);
            }

            var reversed = new string(chars);
            _summary.WithLabels("collect_your_metrics").Observe(1.0);
            return Task.FromResult<MotorCloudEvent<string>?>(dataCloudEvent.CreateNew(reversed));
        }
    }
}
