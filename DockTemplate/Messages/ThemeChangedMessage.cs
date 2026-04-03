using Avalonia.Styling;

namespace DockTemplate.Messages;

public class ThemeChangedMessage(ThemeVariant newTheme)
{
    public ThemeVariant NewTheme { get; } = newTheme;
}