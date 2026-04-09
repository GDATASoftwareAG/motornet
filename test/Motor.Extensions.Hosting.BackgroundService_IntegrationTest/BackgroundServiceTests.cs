using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Motor.Extensions.Hosting.BackgroundService;
using Motor.Extensions.Hosting.BackgroundService_IntegrationTest.TestExample;
using Motor.Extensions.TestUtilities;
using Xunit;

namespace Motor.Extensions.Hosting.BackgroundService_IntegrationTest;

public class BackgroundServiceTests
{
    [Fact]
    public async Task WaitUntilHealthy_UnsafeHostedService_ThrowsException()
    {
        await using var host = MotorTestHost
            .BasedOn<ExampleProgram>()
            .ConfigureServices(services =>
            {
                services.AddHostedService<UnsafeHostedService>();
            })
            .Build();

        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            while (true)
            {
                await host.WaitUntilHealthy();
                await Task.Delay(10);
            }
            // ReSharper disable once FunctionNeverReturns
        });
    }

    private class UnsafeHostedService(ISharedService service) : Microsoft.Extensions.Hosting.BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken ct)
        {
            if (!service.IsStarted())
            {
                throw new Exception("Service not started");
            }

            service.Finish();
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task WaitUntilHealthy_SafeHostedService_StartsSuccessfully()
    {
        await using var host = MotorTestHost
            .BasedOn<ExampleProgram>()
            .ConfigureServices(services =>
            {
                services.AddHostedService<BackgroundStartupTask>();
                services.AddHostedService<SafeHostedService>();
            })
            .Build();

        var sharedService = host.Services.GetRequiredService<ISharedService>();

        await host.WaitUntilHealthy();

        Assert.True(sharedService.IsFinished());
    }

    public class SafeHostedService(
        ISharedService service,
        IHostApplicationLifetime appLifetime,
        ILogger<StartedBackgroundService> logger
    ) : StartedBackgroundService(appLifetime, logger)
    {
        protected override async Task ExecuteWhenStartedAsync(CancellationToken ct)
        {
            if (!service.IsStarted())
            {
                throw new Exception("Service not started");
            }

            service.Finish();
            await Task.Delay(1000, ct);
        }
    }

    public class BackgroundStartupTask(ISharedService sharedService) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            sharedService.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
