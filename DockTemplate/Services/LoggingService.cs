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
        uiTarget.Layout = "${longdate} ${level:uppercase=true} ${logger:shortName=true} ${message} ${exception:format=tostring}";
        
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

            // If it's an error or higher, also add to error list
            if (logEvent.Level >= LogLevel.Error && logEvent.Exception != null)
            {
                var errorEntry = new ErrorEntry
                {
                    Timestamp = logEvent.TimeStamp,
                    Level = logEvent.Level.Name,
                    Message = logEvent.FormattedMessage ?? "Unknown error",
                    Exception = logEvent.Exception.Message,
                    FullException = logEvent.Exception.ToString(),
                    Source = ExtractSourceFromException(logEvent.Exception),
                    Line = ExtractLineFromException(logEvent.Exception),
                    Column = ExtractColumnFromException(logEvent.Exception)
                };

                ErrorEntries.Add(errorEntry);
            }
        }
        catch (Exception ex)
        {
            // Fallback logging to prevent infinite loops
            Console.WriteLine( $"[LoggingService] Error processing log entry: {ex.Message}");
        }
    }

    private string? ExtractSourceFromException(Exception exception)
    {
        try
        {
            var stackTrace = exception.StackTrace;
            if (string.IsNullOrEmpty(stackTrace)) return null;

            // Look for file paths in stack trace
            var lines = stackTrace.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains(".cs:") && line.Contains("\\"))
                {
                    var start = line.LastIndexOf(" in ");
                    if (start >= 0)
                    {
                        var pathPart = line.Substring(start + 4);
                        var end = pathPart.LastIndexOf(":line");
                        if (end >= 0)
                        {
                            return pathPart.Substring(0, end).Trim();
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore extraction errors
        }
        return null;
    }

    private int? ExtractLineFromException(Exception exception)
    {
        try
        {
            var stackTrace = exception.StackTrace;
            if (string.IsNullOrEmpty(stackTrace)) return null;

            var lines = stackTrace.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains(":line "))
                {
                    var start = line.LastIndexOf(":line ") + 6;
                    var end = line.Length;
                    var lineStr = line.Substring(start, end - start).Trim();
                    
                    if (int.TryParse(lineStr, out int lineNumber))
                    {
                        return lineNumber;
                    }
                }
            }
        }
        catch
        {
            // Ignore extraction errors
        }
        return null;
    }

    private int? ExtractColumnFromException(Exception exception)
    {
        // Column information is typically not available in .NET stack traces
        // Could be enhanced with additional debugging information
        return null;
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
    public string Exception { get; set; } = null!;
    public string FullException { get; set; } = null!;
    public string? Source { get; set; }
    public int? Line { get; set; }
    public int? Column { get; set; }
    
    public string FormattedTimestamp => Timestamp.ToString("HH:mm:ss.fff");
    public bool HasSourceLocation => !string.IsNullOrEmpty(Source) && Line.HasValue;
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