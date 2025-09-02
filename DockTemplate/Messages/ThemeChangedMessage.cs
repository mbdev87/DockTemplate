using Avalonia.Styling;

namespace DockTemplate.Messages;

public class ThemeChangedMessage
{
    public ThemeVariant NewTheme { get; }

    public ThemeChangedMessage(ThemeVariant newTheme)
    {
        NewTheme = newTheme;
    }
}