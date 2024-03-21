using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Polly;
using Xunit;

namespace Motor.Extensions.Diagnostics.Telemetry_IntegrationTest;

public class OpenTelemetryCollectorFixture : IAsyncLifetime
{
    private const int OtlpPort = 4317;

    private IContainer? Container { get; set; }

    public Uri Endpoint => new($"http://{Container!.Hostname}:{Container!.GetMappedPublicPort(OtlpPort)}");

    public async Task<bool> ReceivedSpanAsync(string spanId) =>
        await Policy.HandleResult(false)
            .WaitAndRetryAsync(10, _ => TimeSpan.FromSeconds(1))
            .ExecuteAsync(async () =>
            {
                var (_, stderr) = await Container!.GetLogsAsync(timestampsEnabled: false).ConfigureAwait(false);
                return stderr.Contains(spanId);
            });

    public async Task InitializeAsync()
    {
        var dockerImage = new ImageFromDockerfileBuilder()
            .WithDockerfile("OpenTelemetryCollector.Dockerfile")
            .Build();
        await dockerImage.CreateAsync();
        Container = new ContainerBuilder()
            .WithImage(dockerImage)
            .WithPortBinding(OtlpPort, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged(
                @"Everything is ready\. Begin running and processing data\."))
            .Build();
        await Container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return Container!.StopAsync();
    }
}
