using DockComponent.Base;

namespace DockComponent.SolutionExplorer.Messages;

// DUPLICATED MESSAGE - SolutionExplorer component's copy of navigation message  
// This must be duplicated in each component that needs navigation (no shared dependencies!)
public class NavigateToSourceMessage
{
    public string FilePath { get; set; } = "";
    public int LineNumber { get; set; }
    public int Column { get; set; } = 0;
    public bool HighlightLine { get; set; } = true;
    public string? Context { get; set; }  // Optional context like "Opened from Solution Explorer"
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