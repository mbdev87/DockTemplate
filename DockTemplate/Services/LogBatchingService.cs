using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DockComponent.Base;
using Microsoft.Extensions.Logging;
using ReactiveUI;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global

namespace DockTemplate.Services;

public class LogBatchingService(ILogger<LogBatchingService> logger)
    : BackgroundService
{
    private readonly ConcurrentQueue<ErrorEntry> _errorQueue = new();
    private readonly TimeSpan _batchInterval = TimeSpan.FromMilliseconds(100);
    private const int MaxBatchSize = 50;

    public void EnqueueError(ErrorEntry errorEntry)
    {
        _errorQueue.Enqueue(errorEntry);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("LogBatchingService started");

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
                logger.LogError(ex, "Error processing log batch");
            }
        }

        logger.LogInformation("LogBatchingService stopped");
    }

    private async Task ProcessBatch()
    {
        if (_errorQueue.IsEmpty)
            return;

        var batch = new List<ErrorEntry>();

        while (batch.Count < MaxBatchSize &&
               _errorQueue.TryDequeue(out var errorEntry))
        {
            batch.Add(errorEntry);
        }

        if (batch.Count > 0)
        {
            var batchedMessage = new BatchedErrorMessage
            {
                Entries = batch.ToArray()
            };

            await Task.Run(() =>
                MessageBus.Current.SendMessage(batchedMessage));
            logger.LogDebug("Sent batch of {Count} error entries", batch.Count);
        }
    }
}