using System.Collections.ObjectModel;
using NLog;
using NLog.Targets;
using ReactiveUI;

namespace DockComponent.Base;

/// <summary>
/// Shared logging infrastructure that all components can use.
/// Ensures consistent formatting and prevents duplicate UILogTarget instances.
/// </summary>
public interface ISharedLoggingService
{
    void ConfigureComponentLogging(string componentName);
    ObservableCollection<LogEntry> LogEntries { get; }
    ObservableCollection<ErrorEntry> ErrorEntries { get; }
}

public class SharedLoggingService : ISharedLoggingService
{
    private static SharedLoggingService? _instance;
    private static readonly object _lock = new object();
    private static bool _isConfigured = false;
    
    public static SharedLoggingService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new SharedLoggingService();
                    }
                }
            }
            return _instance;
        }
    }
    
    public ObservableCollection<LogEntry> LogEntries { get; } = new();
    public ObservableCollection<ErrorEntry> ErrorEntries { get; } = new();

    private SharedLoggingService()
    {
        // Private constructor for singleton
    }

    public void ConfigureComponentLogging(string componentName)
    {
        lock (_lock)
        {
            if (_isConfigured)
            {
                // Already configured, just log that a new component is using it
                var logger = LogManager.GetLogger(componentName);
                logger.Debug($"[SharedLogging] {componentName} connected to shared logging service");
                return;
            }

            // First component to request logging - configure NLog globally
            ConfigureGlobalNLog();
            _isConfigured = true;
            
            var setupLogger = LogManager.GetLogger("SharedLogging");
            setupLogger.Info($"[SharedLogging] Global logging configured by {componentName}");
        }
    }

    private void ConfigureGlobalNLog()
    {
        var config = LogManager.Configuration ?? new NLog.Config.LoggingConfiguration();
        
        // Create shared UILogTarget with consistent formatting
        var sharedUITarget = new SharedUILogTarget(this);
        sharedUITarget.Name = "SharedUITarget";
        sharedUITarget.Layout =
            "${longdate} ${level:uppercase=true} ${logger:shortName=true} ${message} " +
            "[${callsite-filename}:${callsite-linenumber} ${callsite-method}] " +
            "${exception:format=tostring}";
        
        // Remove any existing UI targets to prevent duplicates
        config.RemoveTarget("UITarget");
        config.RemoveTarget("SharedUITarget");
        
        config.AddTarget(sharedUITarget);
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, sharedUITarget);
        
        LogManager.Configuration = config;
    }

    public void AddLogEntry(LogEventInfo logEvent)
    {
        try
        {
            var entry = new LogEntry(
                logEvent.Level.Name,
                logEvent.FormattedMessage ?? string.Empty,
                logEvent.LoggerName ?? "Unknown", 
                logEvent.TimeStamp
            )
            {
                Exception = logEvent.Exception?.ToString()
            };

            // Add to log entries (keep last 1000 entries)
            // UI components should handle UI thread marshalling themselves
            if (LogEntries.Count >= 1000)
            {
                LogEntries.RemoveAt(0);
            }
            LogEntries.Add(entry);

            // If it's an error/warning/fatal, create error entry and send via message bus
            if (logEvent.Level >= LogLevel.Warn)
            {
                var errorEntry = new ErrorEntry()
                {
                    Message = logEvent.FormattedMessage ?? "Unknown message",
                    Level = logEvent.Level.Name,
                    Code = logEvent.CallerFilePath,
                    Line = logEvent.CallerLineNumber,
                    Exception = logEvent.Exception?.Message,
                    FullException = logEvent.Exception?.ToString(),
                    Source = logEvent.CallerMemberName,
                    LoggerName = logEvent.LoggerName,
                    Timestamp = logEvent.TimeStamp
                };
                
                ErrorEntries.Add(errorEntry);
                
                // Send error via component message bus for ErrorList component
                var errorMessage = new ComponentMessage(
                    "SharedLogging_ErrorReported",
                    System.Text.Json.JsonSerializer.Serialize(errorEntry)
                );
                MessageBus.Current.SendMessage(errorMessage);
            }
        }
        catch (Exception ex)
        {
            // Fallback logging to prevent infinite loops
            Console.WriteLine($"[SharedLoggingService] Error processing log entry: {ex.Message}");
        }
    }
}

// LogEntry and ErrorEntry are now in separate files in DockComponent.Base namespace

/// <summary>
/// Shared NLog target that feeds the SharedLoggingService
/// </summary>
public class SharedUILogTarget : TargetWithLayout
{
    private readonly SharedLoggingService _sharedLoggingService;

    public SharedUILogTarget(SharedLoggingService sharedLoggingService)
    {
        _sharedLoggingService = sharedLoggingService;
    }

    protected override void Write(LogEventInfo logEvent)
    {
        _sharedLoggingService.AddLogEntry(logEvent);
    }
}