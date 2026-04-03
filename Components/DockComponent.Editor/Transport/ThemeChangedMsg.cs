using Avalonia.Styling;

namespace DockComponent.Editor.Transport
{
    public class ThemeChangedMsg(ThemeVariant newTheme)
    {
        public ThemeVariant NewTheme { get; } = newTheme;
    }
}