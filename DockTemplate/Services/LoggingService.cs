using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using NLog;
using NLog.Targets;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace DockTemplate.Services;

public class LoggingService : ReactiveObject
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    [Reactive] public ObservableCollection<LogEntry> LogEntries { get; set; } = new();
    [Reactive] public ObservableCollection<ErrorEntry> ErrorEntries { get; set; } = new();
    
    public LoggingService()
    {
        // Configure NLog with our custom target
        ConfigureNLog();
        Logger.Info("LoggingService initialized");
    }

    private void ConfigureNLog()
    {
        var config = LogManager.Configuration ?? new NLog.Config.LoggingConfiguration();
        
        // Create custom target that feeds our UI
        var uiTarget = new UILogTarget(this);
        uiTarget.Name = "UITarget";
        uiTarget.Layout =
            "${longdate} ${level:uppercase=true} ${logger:shortName=true} ${message} " +
            "[${callsite-filename}:${callsite-linenumber} ${callsite-method}] " +
            "${exception:format=tostring}";
        
        config.AddTarget(uiTarget);
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, uiTarget);
        
        LogManager.Configuration = config;
    }

    public void AddLogEntry(LogEventInfo logEvent)
    {
        try
        {
            var entry = new LogEntry
            {
                Timestamp = logEvent.TimeStamp,
                Level = logEvent.Level.Name,
                Logger = logEvent.LoggerName ?? "Unknown",
                Message = logEvent.FormattedMessage ?? string.Empty,
                Exception = logEvent.Exception?.ToString()
            };

            // Add to log entries (keep last 1000 entries)
            if (LogEntries.Count >= 1000)
            {
                LogEntries.RemoveAt(0);
            }
            LogEntries.Add(entry);

            // If it's an error/warning/fatal, publish to MessageBus for Error List
            if (logEvent.Level >= LogLevel.Warn)
            {
                var errorMessage = new ErrorMessage
                {
                  Entry = new ErrorEntry()
                  {
                      Message  = logEvent.FormattedMessage ?? "Unknown message",
                      Level = logEvent.Level.Name,
                      Code = logEvent.CallerFilePath,
                      Line = logEvent.CallerLineNumber,
                      Exception = logEvent.Exception?.Message,
                      FullException = logEvent.Exception?.ToString(),
                      Source = logEvent.CallerMemberName,
                      LoggerName = logEvent.LoggerName,
                      Timestamp = logEvent.TimeStamp
                  }
                };
                ErrorEntries.Add(errorMessage.Entry);   
                MessageBus.Current.SendMessage(errorMessage);
            }
        }
        catch (Exception ex)
        {
            // Fallback logging to prevent infinite loops
            Console.WriteLine( $"[LoggingService] Error processing log entry: {ex.Message}");
        }
    }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = null!;
    public string Logger { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? Exception { get; set; }
    
    public string FormattedTimestamp => Timestamp.ToString("HH:mm:ss.fff");
    public string FormattedMessage => $"[{FormattedTimestamp}] {Level.ToUpper()} {Logger}: {Message}";
}

public class ErrorEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? Exception { get; set; } = null!;
    public string? FullException { get; set; } = null!;
    public string? Source { get; set; }
    public int? Line { get; set; }
    
    public string FormattedTimestamp => Timestamp.ToString("HH:mm:ss.fff");
    public bool HasSourceLocation => !string.IsNullOrEmpty(Source) && Line.HasValue;
    public string? Code { get; set; }
    public string? LoggerName { get; set; }
}

// Custom NLog target that feeds our UI
public class UILogTarget : TargetWithLayout
{
    private readonly LoggingService _loggingService;

    public UILogTarget(LoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    protected override void Write(LogEventInfo logEvent)
    {
        _loggingService.AddLogEntry(logEvent);
    }
}