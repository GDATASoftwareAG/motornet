using System;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Moq;
using Motor.Extensions.Diagnostics.Logging;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.Hosting.Publisher;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Hosting.RabbitMQ_IntegrationTest;
using Motor.Extensions.Http;
using Motor.Extensions.Utilities;
using Polly;
using RandomDataGenerator.FieldOptions;
using RandomDataGenerator.Randomizers;
using Xunit;

namespace Motor.Extensions.Utilities_IntegrationTest;

[Collection("GenericHosting")]
public class GenericHostingTests(RabbitMQFixture fixture)
    : GenericHostingTestBase(fixture),
        IClassFixture<RabbitMQFixture>
{
    [Fact(Timeout = 60000)]
    public async Task StartAsync_UseConfigureDefaultMessageHandlerWithMessageProcessingHealthCheck_HealthCheckUnhealthy()
    {
        const string maxTimeSinceLastProcessedMessage = "00:00:00.1";
        Environment.SetEnvironmentVariable(
            "HealthChecks__MessageProcessingHealthCheck__MaxTimeSinceLastProcessedMessage",
            maxTimeSinceLastProcessedMessage
        );

        var messageCount = Environment.ProcessorCount + 1;
        using var host = GetStringService<TimingOutMessageConverter>(
            listenerPort: RandomHttpPort,
            prefetchCount: messageCount
        );
        await using var channel = await (await Fixture.ConnectionAsync()).CreateChannelAsync();
        await CreateQueueForServicePublisherWithPublisherBindingFromConfigAsync(channel);
        await host.StartAsync();

        await Task.Run(async () =>
        {
            foreach (var _ in Enumerable.Range(0, messageCount))
            {
                await PublishMessageIntoQueueOfServiceAsync(channel, "somestring");
            }
        });

        using var httpClient = HttpClient();

        await WaitUntilAsync(async () =>
        {
            using var healthResponse = await httpClient.GetAsync($"http://localhost:{RandomHttpPort}/health");
            Assert.Equal(HttpStatusCode.ServiceUnavailable, healthResponse.StatusCode);
            Assert.Equal(nameof(HealthStatus.Unhealthy), await healthResponse.Content.ReadAsStringAsync());
        });
        await host.StopAsync();
    }

    [Fact(Timeout = 60000)]
    public async Task StartAsync_UseConfigureDefaultMessageHandlerWithMessageProcessingHealthCheck_HealthCheckHealthy()
    {
        const string maxTimeSinceLastProcessedMessage = "00:01:00";
        Environment.SetEnvironmentVariable(
            "HealthChecks__MessageProcessingHealthCheck__MaxTimeSinceLastProcessedMessage",
            maxTimeSinceLastProcessedMessage
        );
        var messageCount = Environment.ProcessorCount + 1;
        const string message = "somestring";
        using var host = GetStringService<TimingOutMessageConverter>(
            listenerPort: RandomHttpPort,
            prefetchCount: messageCount
        );
        var channel = await (await Fixture.ConnectionAsync()).CreateChannelAsync();
        await CreateQueueForServicePublisherWithPublisherBindingFromConfigAsync(channel);
        await host.StartAsync();
        for (var i = 0; i < messageCount; i++)
        {
            await PublishMessageIntoQueueOfServiceAsync(channel, message);
        }

        using var httpClient = HttpClient();

        var healthResponse = await httpClient.GetAsync($"http://localhost:{RandomHttpPort}/health");

        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
        Assert.Equal(nameof(HealthStatus.Healthy), await healthResponse.Content.ReadAsStringAsync());
        await host.StopAsync();
    }

    [Fact(Timeout = 60000)]
    public async Task StartAsync_UseConfigureDefaultMessageHandlerWithTooManyTemporaryFailuresHealthCheck_HealthCheckUnhealthy()
    {
        const int messageCount = 20;
        const string message = "somestring";
        using var host = GetStringService<TemporaryFailingConverter>(
            listenerPort: RandomHttpPort,
            prefetchCount: messageCount
        );
        var channel = await (await Fixture.ConnectionAsync()).CreateChannelAsync();
        await CreateQueueForServicePublisherWithPublisherBindingFromConfigAsync(channel);
        await host.StartAsync();
        for (var i = 0; i < messageCount; i++)
        {
            await PublishMessageIntoQueueOfServiceAsync(channel, message);
        }

        using var httpClient = HttpClient();

        await WaitUntilAsync(async () =>
        {
            var healthResponse = await httpClient.GetAsync($"http://localhost:{RandomHttpPort}/health");
            Assert.Equal(HttpStatusCode.ServiceUnavailable, healthResponse.StatusCode);
            Assert.Equal(nameof(HealthStatus.Unhealthy), await healthResponse.Content.ReadAsStringAsync());
        });
        await host.StopAsync();
    }

    [Fact(Timeout = 60000)]
    public async Task StartAsync_UseConfigureDefaultMessageHandlerWithTooManyTemporaryFailuresHealthCheck_HealthCheckHealthy()
    {
        const int messageCount = 20;
        const string message = "somestring";
        using var host = GetStringService<SometimesFailingConverter>(
            listenerPort: RandomHttpPort,
            prefetchCount: messageCount
        );
        var channel = await (await Fixture.ConnectionAsync()).CreateChannelAsync();
        await CreateQueueForServicePublisherWithPublisherBindingFromConfigAsync(channel);
        await host.StartAsync();
        for (var i = 0; i < messageCount; i++)
        {
            await PublishMessageIntoQueueOfServiceAsync(channel, message);
        }

        using var httpClient = HttpClient();

        await WaitUntilAsync(async () =>
        {
            var healthResponse = await httpClient.GetAsync($"http://localhost:{RandomHttpPort}/health");

            Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
            Assert.Equal(nameof(HealthStatus.Healthy), await healthResponse.Content.ReadAsStringAsync());
        });
        await host.StopAsync();
    }

    private IHost GetStringService<TConverter>(ushort listenerPort = 9110, int prefetchCount = 1)
        where TConverter : class, ISingleOutputService<string, string>
    {
        var randomizerString = RandomizerFactory.GetRandomizer(new FieldOptionsTextRegex { Pattern = @"^[A-Z]{10}" });

        return new MotorHostBuilder(new HostBuilder())
            .UseSetting(MotorHostDefaults.EnablePrometheusEndpointKey, false.ToString())
            .ConfigureSerilog()
            .ConfigurePrometheus()
            .ConfigureSingleOutputService<string, string>()
            .ConfigureServices(
                (_, services) =>
                {
                    services.AddTransient(_ =>
                    {
                        var mock = new Mock<IApplicationNameService>();
                        mock.Setup(t => t.GetVersion()).Returns("test");
                        mock.Setup(t => t.GetLibVersion()).Returns("test");
                        mock.Setup(t => t.GetSource()).Returns(new Uri("motor://test"));
                        return mock.Object;
                    });
                    services.AddTransient<ISingleOutputService<string, string>, TConverter>();
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
            .ConfigureAppConfiguration(
                (_, config) =>
                {
                    config.AddJsonFile("appsettings.json", true, false);
                    config.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            { "ASPNETCORE_URLS", $"http://0.0.0.0:{listenerPort}" },
                            { "Prometheus__Port", $"http://0.0.0.0:{listenerPort}" },
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
                }
            )
            .ConfigureDefaultHttpClient()
            .Build();
    }

    private static HttpClient HttpClient() => new() { Timeout = TimeSpan.FromSeconds(5) };

    private static Task WaitUntilAsync(Func<Task> action) =>
        Policy
            .Handle<Exception>()
            .WaitAndRetryForeverAsync(_ => TimeSpan.FromMilliseconds(250))
            .ExecuteAsync(async () => await action());

    private class TimingOutMessageConverter : ISingleOutputService<string, string>
    {
        public async Task<MotorCloudEvent<string>?> ConvertMessageAsync(
            MotorCloudEvent<string> dataCloudEvent,
            CancellationToken token = default
        )
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token).ConfigureAwait(false);
            return dataCloudEvent.CreateNew(string.Empty);
        }
    }

    private class TemporaryFailingConverter : ISingleOutputService<string, string>
    {
        public Task<MotorCloudEvent<string>?> ConvertMessageAsync(
            MotorCloudEvent<string> dataCloudEvent,
            CancellationToken token = default
        )
        {
            throw new TemporaryFailureException();
        }
    }

    private class SometimesFailingConverter : ISingleOutputService<string, string>
    {
        private static int _consumedCounter;

        public Task<MotorCloudEvent<string>?> ConvertMessageAsync(
            MotorCloudEvent<string> dataCloudEvent,
            CancellationToken token = default
        )
        {
            var incrementedCounter = Interlocked.Increment(ref _consumedCounter);
            if (incrementedCounter % 2 == 0)
            {
                throw new TemporaryFailureException();
            }

            return Task.FromResult<MotorCloudEvent<string>?>(dataCloudEvent.CreateNew(string.Empty));
        }
    }
}
