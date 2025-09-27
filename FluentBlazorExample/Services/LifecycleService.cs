using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FluentBlazorExample.Services;

public class LifecycleService : BackgroundService
{
    private readonly ILogger<LifecycleService> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private DateTime _lastHeartbeat = DateTime.UtcNow;
    private readonly TimeSpan _heartbeatTimeout = TimeSpan.FromSeconds(10); // 2 missed 3-second intervals + buffer

    public LifecycleService(ILogger<LifecycleService> logger, IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _lifetime = lifetime;
    }

    public void UpdateHeartbeat()
    {
        _lastHeartbeat = DateTime.UtcNow;
        _logger.LogDebug("Heartbeat updated at {Timestamp}", _lastHeartbeat);
    }

    public void TriggerShutdown()
    {
        _logger.LogInformation("Shutdown triggered via endpoint");
        _lifetime.StopApplication();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Lifecycle monitoring started - checking every 5 seconds");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(5000, stoppingToken); // Check every 5 seconds

                var timeSinceLastHeartbeat = DateTime.UtcNow - _lastHeartbeat;

                if (timeSinceLastHeartbeat > _heartbeatTimeout)
                {
                    _logger.LogWarning("Heartbeat timeout detected! Last heartbeat: {LastHeartbeat}, Time since: {TimeSince}",
                        _lastHeartbeat, timeSinceLastHeartbeat);

                    _logger.LogInformation("Initiating self-shutdown due to heartbeat timeout");
                    _lifetime.StopApplication();
                    break;
                }

                _logger.LogTrace("Heartbeat check passed - last heartbeat: {TimeSince} ago", timeSinceLastHeartbeat);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in heartbeat monitoring");
            }
        }

        _logger.LogInformation("Lifecycle monitoring stopped");
    }
}