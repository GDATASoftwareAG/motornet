using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Motor.Extensions.Hosting.BackgroundService;

public abstract class StartedBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
{
    protected StartedBackgroundService(IHostApplicationLifetime appLifetime, ILogger<StartedBackgroundService> logger)
    {
        _appLifetime = appLifetime;
        _logger = logger;
    }

    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger<StartedBackgroundService> _logger;
    protected abstract Task ExecuteWhenStartedAsync(CancellationToken stoppingToken);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            if (!await AppStartedSuccessfullyAsync(ct))
            {
                _logger.LogError(LogEvents.FailedToStart, "Failed to start successfully");
                return;
            }
            await ExecuteWhenStartedAsync(ct);
            _logger.LogInformation(LogEvents.FinishedSuccessfully, "Finished Successfully");
        }
        catch (Exception e)
        {
            _logger.LogCritical(LogEvents.Failure, e, "Something went wrong: {Error}", e.Message);
            Environment.ExitCode = 1;
        }
        finally
        {
            _appLifetime.StopApplication();
        }
    }

    /// <summary>
    /// Waits until the app is fully started and returns the success.
    /// This is important because <tt>StopApplication()</tt> should never be called before an app is started completely.
    /// </summary>
    /// <returns><tt>true</tt> if started successfully, <tt>false</tt> otherwise</returns>
    private async Task<bool> AppStartedSuccessfullyAsync(CancellationToken stoppingToken)
    {
        // Taken from https://andrewlock.net/finding-the-urls-of-an-aspnetcore-app-from-a-hosted-service-in-dotnet-6/
        var startedSource = new TaskCompletionSource();
        var cancelledSource = new TaskCompletionSource();

        await using (_appLifetime.ApplicationStarted.Register(() => startedSource.SetResult()))
        await using (stoppingToken.Register(() => cancelledSource.SetResult()))
        {
            var completedTask = await Task.WhenAny(startedSource.Task, cancelledSource.Task).ConfigureAwait(false);

            // If the completed task was the "app started" task, return true, otherwise false
            return completedTask == startedSource.Task;
        }
    }
}
