using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Moq;
using Motor.Extensions.Diagnostics.Logging;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.Hosting.Publisher;
using Motor.Extensions.Hosting.RabbitMQ;
using Motor.Extensions.Hosting.RabbitMQ_IntegrationTest;
using Motor.Extensions.Http;
using Motor.Extensions.Utilities;
using Xunit;

namespace Motor.Extensions.Utilities_IntegrationTest
{
    [Collection("GenericHosting")]
    public class GenericHostingTests : GenericHostingTestBase, IClassFixture<RabbitMQFixture>
    {
        public GenericHostingTests(RabbitMQFixture fixture)
            : base(fixture)
        {
        }

        [Fact(Timeout = 60000)]
        public async Task
            StartAsync_UseConfigureDefaultMessageHandlerWithMessageProcessingHealthCheck_HealthCheckUnhealthy()
        {
            const string maxTimeSinceLastProcessedMessage = "00:00:00.5";
            Environment.SetEnvironmentVariable("HealthCheck__MaxTimeSinceLastProcessedMessage",
                maxTimeSinceLastProcessedMessage);
            var messageCount = Environment.ProcessorCount + 1;
            PrepareQueues(messageCount);
            const string message = "somestring";
            using var host = GetStringService<TimingOutMessageConverter>();
            var channel = Fixture.Connection.CreateModel();
            await CreateQueueForServicePublisherWithPublisherBindingFromConfig(channel);
            await host.StartAsync();
            for (var i = 0; i < messageCount; i++) PublishMessageIntoQueueOfService(channel, message);
            var httpClient = new HttpClient();

            await Task.Delay(TimeSpan.Parse(maxTimeSinceLastProcessedMessage) * 2);
            var healthResponse = await httpClient.GetAsync("http://localhost:9110/health");

            Assert.Equal(HttpStatusCode.ServiceUnavailable, healthResponse.StatusCode);
            Assert.Equal(HealthStatus.Unhealthy.ToString(), await healthResponse.Content.ReadAsStringAsync());
            await host.StopAsync();
        }

        [Fact(Timeout = 60000)]
        public async Task
            StartAsync_UseConfigureDefaultMessageHandlerWithMessageProcessingHealthCheck_HealthCheckHealthy()
        {
            const string maxTimeSinceLastProcessedMessage = "00:01:00";
            Environment.SetEnvironmentVariable("HealthCheck__MaxTimeSinceLastProcessedMessage",
                maxTimeSinceLastProcessedMessage);
            var messageCount = Environment.ProcessorCount + 1;
            PrepareQueues(messageCount);
            const string message = "somestring";
            using var host = GetStringService<TimingOutMessageConverter>();
            var channel = Fixture.Connection.CreateModel();
            await CreateQueueForServicePublisherWithPublisherBindingFromConfig(channel);
            await host.StartAsync();
            for (var i = 0; i < messageCount; i++) PublishMessageIntoQueueOfService(channel, message);
            var httpClient = new HttpClient();

            var healthResponse = await httpClient.GetAsync("http://localhost:9110/health");

            Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
            Assert.Equal(HealthStatus.Healthy.ToString(), await healthResponse.Content.ReadAsStringAsync());
            await host.StopAsync();
        }

        private IHost GetStringService<TConverter>() where TConverter : class, ISingleOutputService<string, string>
        {
            var host = new MotorHostBuilder(new HostBuilder())
                .UseSetting(MotorHostDefaults.EnablePrometheusEndpointKey, false.ToString())
                .ConfigureSerilog()
                .ConfigurePrometheus()
                .ConfigureSingleOutputService<string, string>()
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
                    services.AddTransient<ISingleOutputService<string, string>, TConverter>();
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
                .ConfigureDefaultHttpClient()
                .Build();
            return host;
        }

        private class TimingOutMessageConverter : ISingleOutputService<string, string>
        {
            public async Task<MotorCloudEvent<string>> ConvertMessageAsync(MotorCloudEvent<string> dataCloudEvent,
                CancellationToken token = default)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return dataCloudEvent;
            }
        }
    }
}
