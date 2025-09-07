using DockComponent.Base;

namespace DockComponent.ErrorList.Messages;

// Inter-plugin message for error entries
public class ErrorMessage
{
    public string Level { get; set; } = ""; // Error, Warning, Info
    public string Code { get; set; } = "";  // Error code like CS0103
    public string Description { get; set; } = "";
    public string Project { get; set; } = "";
    public string FilePath { get; set; } = "";
    public int LineNumber { get; set; }
    public int Column { get; set; }
    public DateTime Timestamp { get; set; }
}

// ComponentMessage wrapper for transport
public static class ErrorMessageTransport
{
    public const string MESSAGE_NAME = "ErrorList_ErrorMessage";
    public const int VERSION = 1;
    
    public static ComponentMessage Create(ErrorMessage errorMessage)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(errorMessage);
        return new ComponentMessage(MESSAGE_NAME, json, VERSION);
    }
    
    public static ErrorMessage? Parse(ComponentMessage message)
    {
        if (message.Name != MESSAGE_NAME) return null;
        
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<ErrorMessage>(message.Payload);
        }
        catch
        {
            return null;
        }
    }
}