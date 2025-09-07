using System.Text.Json;

namespace DockComponent.Editor.Transport.ErrorListComponent;

/// <summary>
/// Message we CONSUME from ErrorList component when user clicks an error
/// Contract: ErrorListComponent_ErrorClicked (v1)
/// Copy/pasted from ErrorList team's documentation
/// </summary>
public record ErrorClickedMsg(string FilePath, int LineNumber, string ErrorMessage, string ErrorLevel, DateTime Timestamp);

public static class ErrorClickedHelper
{
    public static ErrorClickedMsg? TryParse(string messageData)
    {
        try
        {
            return JsonSerializer.Deserialize<ErrorClickedMsg>(messageData);
        }
        catch
        {
            return null;
        }
    }
}