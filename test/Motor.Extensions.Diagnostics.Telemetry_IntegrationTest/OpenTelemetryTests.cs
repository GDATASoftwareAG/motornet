using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Motor.Extensions.Conversion.Abstractions;
using Motor.Extensions.Diagnostics.Logging;
using Motor.Extensions.Diagnostics.Metrics;
using Motor.Extensions.Diagnostics.Telemetry;
using Motor.Extensions.Hosting;
using Motor.Extensions.Hosting.Abstractions;
using Motor.Extensions.Hosting.CloudEvents;
using Motor.Extensions.Hosting.Consumer;
using Motor.Extensions.Hosting.Publisher;
using Motor.Extensions.TestUtilities;
using Motor.Extensions.Utilities;
using Motor.Extensions.Utilities.Abstractions;
using OpenTelemetry.Exporter;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Motor.Extensions.Diagnostics.Telemetry_IntegrationTest;

public class OpenTelemetryTests : IDisposable, IClassFixture<OpenTelemetryCollectorFixture>
{
    private readonly ActivityListener _listener;
    private readonly OpenTelemetryCollectorFixture _otlpFixture;

    public OpenTelemetryTests(ITestOutputHelper outputHelper, OpenTelemetryCollectorFixture otlpFixture)
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == OpenTelemetryOptions.DefaultActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = _ => { outputHelper.WriteLine("test"); }
        };
        ActivitySource.AddActivityListener(_listener);
        _otlpFixture = otlpFixture;
    }

    [Fact]
    public async Task StartAsync_ConfiguredOtlp_TracePublishedWithOtlp()
    {
        var consumer = new InMemoryConsumer<string>();
        var publisher = new InMemoryPublisher<string>();
        using var host = GetReverseStringService(consumer, publisher, "appsettings-otlp.json", builder =>
            builder.ConfigureServices((_, services) =>
            {
                services.Configure<OtlpExporterOptions>(options => options.Endpoint = _otlpFixture.Endpoint);
            }));
        await host.StartAsync();
        var randomActivity = CreateRandomActivity();
        var motorCloudEvent = MotorCloudEvent.CreateTestCloudEvent(Array.Empty<byte>());
        motorCloudEvent.SetActivity(randomActivity);

        await consumer.AddMessage(motorCloudEvent);

        var ctx = publisher.Events.First().GetActivityContext();
        Assert.True(await _otlpFixture.ReceivedSpanAsync(ctx.SpanId.ToString()));
        await host.StopAsync();
    }

    private static Activity CreateRandomActivity()
    {
        var activity = new Activity(nameof(CreateRandomActivity));
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();
        return activity;
    }

    private static IHost GetReverseStringService(InMemoryConsumer<string> consumer, InMemoryPublisher<string> publisher,
        string? appSettings, params Func<IMotorHostBuilder, IMotorHostBuilder>[] extraConfigurations)
    {
        var motorHostBuilder = new MotorHostBuilder(new HostBuilder(), false)
            .UseSetting(MotorHostDefaults.EnablePrometheusEndpointKey, false.ToString())
            .ConfigureSerilog()
            .ConfigurePrometheus()
            .ConfigureOpenTelemetry()
            .ConfigureServices((_, services) =>
            {
                services.AddTransient(_ =>
                {
                    var mock = new Mock<IApplicationNameService>();
                    mock.Setup(t => t.GetFullName()).Returns("test");
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
            });
        motorHostBuilder = extraConfigurations.Aggregate(motorHostBuilder, (builder, func) => func.Invoke(builder));
        if (appSettings is not null)
        {
            motorHostBuilder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddJsonFile(appSettings, false, false);
            });
        }
        return motorHostBuilder.Build();
    }


    public void Dispose()
    {
        _listener.Dispose();
    }
}
