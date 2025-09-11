using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DockComponent.Base;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace DockTemplate.Services;

public class LogBatchingService : BackgroundService
{
    private readonly ConcurrentQueue<ErrorEntry> _errorQueue = new();
    private readonly ILogger<LogBatchingService> _logger;
    private readonly TimeSpan _batchInterval = TimeSpan.FromMilliseconds(100);
    private const int MaxBatchSize = 50;

    public LogBatchingService(ILogger<LogBatchingService> logger)
    {
        _logger = logger;
    }

    public void EnqueueError(ErrorEntry errorEntry)
    {
        _errorQueue.Enqueue(errorEntry);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LogBatchingService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_batchInterval, stoppingToken);
                await ProcessBatch();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing log batch");
            }
        }

        _logger.LogInformation("LogBatchingService stopped");
    }

    private async Task ProcessBatch()
    {
        if (_errorQueue.IsEmpty)
            return;

        var batch = new List<ErrorEntry>();
        
        while (batch.Count < MaxBatchSize && _errorQueue.TryDequeue(out var errorEntry))
        {
            batch.Add(errorEntry);
        }

        if (batch.Count > 0)
        {
            var batchedMessage = new BatchedErrorMessage
            {
                Entries = batch.ToArray()
            };

            await Task.Run(() => MessageBus.Current.SendMessage(batchedMessage));
            _logger.LogDebug("Sent batch of {Count} error entries", batch.Count);
        }
    }
}