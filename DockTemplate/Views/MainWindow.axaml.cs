using Avalonia.Controls;
using DockTemplate.Services;

namespace DockTemplate.Views;

public partial class MainWindow : Window
{
    private bool _isDark = false;

    public MainWindow()
    {
        InitializeComponent();
        InitializeThemes();
    }

    private void InitializeThemes()
    {
        if (ThemeButton is not null)
        {
            ThemeButton.Click += (_, _) =>
            {
                _isDark = !_isDark;
                App.ThemeService?.Switch(_isDark ? 1 : 0);
            };
        }
    }
}