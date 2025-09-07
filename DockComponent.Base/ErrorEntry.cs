using System;

namespace DockComponent.Base;

/// <summary>
/// Generic error entry used by all components - shared contract in Base
/// </summary>
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