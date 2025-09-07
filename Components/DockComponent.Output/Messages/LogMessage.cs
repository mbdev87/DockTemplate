using DockComponent.Base;

namespace DockComponent.Output.Messages;

// Inter-plugin message for log entries
public class LogMessage
{
    public string Level { get; set; } = "";
    public string Message { get; set; } = "";
    public string Source { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string? FilePath { get; set; }  // Optional - for clickable logs that navigate to source
    public int? LineNumber { get; set; }   // Optional - for line navigation
}

// ComponentMessage wrapper for transport
public static class LogMessageTransport
{
    public const string MESSAGE_NAME = "Output_LogMessage";
    public const int VERSION = 1;
    
    public static ComponentMessage Create(LogMessage logMessage)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(logMessage);
        return new ComponentMessage(MESSAGE_NAME, json, VERSION);
    }
    
    public static LogMessage? Parse(ComponentMessage message)
    {
        if (message.Name != MESSAGE_NAME) return null;
        
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<LogMessage>(message.Payload);
        }
        catch
        {
            return null;
        }
    }
}

// DUPLICATED MESSAGE - Output component's copy of navigation message
// This must be duplicated in each component that needs navigation (no shared dependencies!)
public class NavigateToSourceMessage
{
    public string FilePath { get; set; } = "";
    public int LineNumber { get; set; }
    public int Column { get; set; } = 0;
    public bool HighlightLine { get; set; } = true;
    public string? Context { get; set; }  // Optional context like error description
}

// ComponentMessage wrapper for transport
public static class NavigateToSourceMessageTransport
{
    public const string MESSAGE_NAME = "Editor_NavigateToSource";
    public const int VERSION = 1;
    
    public static ComponentMessage Create(NavigateToSourceMessage navMessage)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(navMessage);
        return new ComponentMessage(MESSAGE_NAME, json, VERSION);
    }
    
    public static NavigateToSourceMessage? Parse(ComponentMessage message)
    {
        if (message.Name != MESSAGE_NAME) return null;
        
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<NavigateToSourceMessage>(message.Payload);
        }
        catch
        {
            return null;
        }
    }
}