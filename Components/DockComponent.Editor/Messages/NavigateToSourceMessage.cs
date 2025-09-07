using DockComponent.Base;

namespace DockComponent.Editor.Messages;

// Message sent when user wants to navigate to a specific source location
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