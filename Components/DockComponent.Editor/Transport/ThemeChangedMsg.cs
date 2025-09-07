using Avalonia.Styling;

namespace DockComponent.Editor.Transport
{
    public class ThemeChangedMsg
    {
        public ThemeVariant NewTheme { get; }

        public ThemeChangedMsg(ThemeVariant newTheme)
        {
            NewTheme = newTheme;
        }
    }
}