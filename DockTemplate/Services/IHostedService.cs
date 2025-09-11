using System;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace DockTemplate.Services;

public interface IHostedService
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

public abstract class BackgroundService : IHostedService, IDisposable
{
    private Task? _executingTask;
    private readonly CancellationTokenSource _stoppingCts = new();
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _executingTask = Task.Factory.StartNew(
                async () =>
                {
                    try
                    {
                        await ExecuteAsync(_stoppingCts.Token);
                    }
                    catch (OperationCanceledException) when (_stoppingCts.Token.IsCancellationRequested)
                    {
                        // Expected during shutdown
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"Background service failed: {ex.Message}");
                        throw;
                    }
                },
                _stoppingCts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default)
            .Unwrap();

        return Task.CompletedTask;
    }


    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_executingTask == null) return;

        await _stoppingCts.CancelAsync();
        await Task.WhenAny(_executingTask, Task.Delay(TimeSpan.FromSeconds(30), cancellationToken));
    }

    protected abstract Task ExecuteAsync(CancellationToken stoppingToken);

    public virtual void Dispose()
    {
        _stoppingCts?.Dispose();
    }
}