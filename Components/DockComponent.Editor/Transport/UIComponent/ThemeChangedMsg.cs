using System.Text.Json;

namespace DockComponent.Editor.Transport.UIComponent;

/// <summary>
/// Message we CONSUME from UI component when theme changes
/// Contract: UIComponent_ThemeChanged (v1)
/// Copy/pasted from UI team's documentation
/// </summary>
public record ThemeChangedMsg(string ThemeName, DateTime Timestamp);

public static class ThemeChangedHelper
{
    public static ThemeChangedMsg? TryParse(string messageData)
    {
        try
        {
            return JsonSerializer.Deserialize<ThemeChangedMsg>(messageData);
        }
        catch
        {
            return null;
        }
    }
}