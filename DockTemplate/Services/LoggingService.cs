using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using NLog;
using NLog.Targets;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using DockComponent.Base;

namespace DockTemplate.Services;

public class LoggingService : ReactiveObject
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly LogBatchingService? _batchingService;
    
    [Reactive] public ObservableCollection<LogEntry> LogEntries { get; set; } = new();
    [Reactive] public ObservableCollection<ErrorEntry> ErrorEntries { get; set; } = new();
    
    public LoggingService(LogBatchingService? batchingService = null)
    {
        _batchingService = batchingService;
        
        // Connect to SharedLoggingService and configure global logging
        ConnectToSharedLogging();
        Logger.Info("LoggingService initialized and connected to SharedLoggingService");
    }

    private void ConnectToSharedLogging()
    {
        // Initialize shared logging service for host application
        SharedLoggingService.Instance.ConfigureComponentLogging("HostApplication");
        
        // Sync our collections with the shared service
        LogEntries = SharedLoggingService.Instance.LogEntries;
        ErrorEntries = SharedLoggingService.Instance.ErrorEntries;
        
        Logger.Info("LoggingService connected to SharedLoggingService - all components will use consistent logging");
    }

    // Note: AddLogEntry is now handled by SharedLoggingService
    // The host LoggingService acts as a bridge to the SharedLoggingService
    // All log processing is centralized in SharedLoggingService for consistency
}

// Note: LogEntry, ErrorEntry, and SharedUILogTarget are now in DockComponent.Base.SharedLogging
// This ensures all components use the same shared logging infrastructure