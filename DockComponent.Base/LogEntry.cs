using System;

namespace DockComponent.Base;

/// <summary>
/// Generic log entry used by all components - shared contract in Base
/// </summary>
public record LogEntry(string Level, string Message, string Source, DateTime Timestamp, string? FilePath = null, int? LineNumber = null)
{
    public string FormattedTimestamp => Timestamp.ToString("HH:mm:ss.fff");
    public string? Exception { get; init; }
    
    // Navigation support
    public bool IsNavigable => !string.IsNullOrEmpty(FilePath) && LineNumber.HasValue;
    public string NavigationText => IsNavigable ? $"{System.IO.Path.GetFileName(FilePath)}:{LineNumber}" : "";
    
    // For Output component display
    public string FormattedMessage => $"[{FormattedTimestamp}] {Level.ToUpper()} {Source}: {Message}";
}