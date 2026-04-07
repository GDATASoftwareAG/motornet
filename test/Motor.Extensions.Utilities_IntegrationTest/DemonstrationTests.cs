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
using RandomDataGenerator.FieldOptions;
using RandomDataGenerator.Randomizers;
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
        var channel = await (await Fixture.ConnectionAsync()).CreateChannelAsync();
        await CreateQueueForServicePublisherWithPublisherBindingFromConfigAsync(channel);

        await host.StartAsync();
        await PublishMessageIntoQueueOfServiceAsync(channel, message);

        var actual = await GetMessageFromDestinationQueue(channel);
        Assert.Equal("54321", actual);
        await host.StopAsync();
    }

    private IHost GetReverseStringService(ushort listenerPort, int prefetchCount = 1)
    {
        var randomizerString = RandomizerFactory.GetRandomizer(new FieldOptionsTextRegex { Pattern = @"^[A-Z]{10}" });

        return MotorHost
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
            .ConfigureHostConfiguration(config =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        { "ASPNETCORE_URLS", $"http://0.0.0.0:{listenerPort}" },
                        { "Prometheus__Port", $"http://0.0.0.0:{listenerPort}" },
                    }
                );
            })
            .ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        { "RabbitMQConsumer__Port", Fixture.Port.ToString() },
                        { "RabbitMQConsumer__Host", Fixture.Hostname },
                        { "RabbitMQConsumer__Queue__Name", randomizerString.Generate() },
                        { "RabbitMQConsumer__PrefetchCount", prefetchCount.ToString() },
                        { "RabbitMQPublisher__PublishingTarget__RoutingKey", randomizerString.Generate() },
                        { "RabbitMQPublisher__Port", Fixture.Port.ToString() },
                        { "RabbitMQPublisher__Host", Fixture.Hostname },
                        { "DestinationQueueName", randomizerString.Generate() },
                    }
                );
            })
            .Build();
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
