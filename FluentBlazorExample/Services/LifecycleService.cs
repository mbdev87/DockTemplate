using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FluentBlazorExample.Services;

public class LifecycleService(
    ILogger<LifecycleService> logger,
    IHostApplicationLifetime lifetime)
    : BackgroundService
{
    private DateTime _lastHeartbeat = DateTime.UtcNow;

    private readonly TimeSpan
        _heartbeatTimeout =
            TimeSpan.FromSeconds(10); // 2 missed 3-second intervals + buffer

    public void UpdateHeartbeat()
    {
        _lastHeartbeat = DateTime.UtcNow;
        logger.LogDebug("Heartbeat updated at {Timestamp}", _lastHeartbeat);
    }

    public void TriggerShutdown()
    {
        logger.LogInformation("Shutdown triggered via endpoint");
        lifetime.StopApplication();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Lifecycle monitoring started - checking every 5 seconds");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(5000, stoppingToken); // Check every 5 seconds

                var timeSinceLastHeartbeat = DateTime.UtcNow - _lastHeartbeat;

                if (timeSinceLastHeartbeat > _heartbeatTimeout)
                {
                    logger.LogWarning(
                        "Heartbeat timeout detected! Last heartbeat: {LastHeartbeat}, Time since: {TimeSince}",
                        _lastHeartbeat, timeSinceLastHeartbeat);

                    logger.LogInformation(
                        "Initiating self-shutdown due to heartbeat timeout");
                    lifetime.StopApplication();
                    break;
                }

                logger.LogTrace(
                    "Heartbeat check passed - last heartbeat: {TimeSince} ago",
                    timeSinceLastHeartbeat);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in heartbeat monitoring");
            }
        }

        logger.LogInformation("Lifecycle monitoring stopped");
    }
}