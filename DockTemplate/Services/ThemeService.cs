using System;
using Avalonia;
using Avalonia.Styling;
using ReactiveUI;
using DockTemplate.Messages;
using NLog;

namespace DockTemplate.Services;

public class ThemeService : IThemeService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public ThemeService()
    {
        // TextMate service integration removed - handled by Editor component
    }
    public void Switch(int index)
    {
        if (Application.Current is null)
        {
            return;
        }

        var newTheme = index switch
        {
            0 => ThemeVariant.Light,
            1 => ThemeVariant.Dark,
            _ => Application.Current.RequestedThemeVariant ?? ThemeVariant.Default
        };

        Logger.Info($"[ThemeService] Switching theme to: {newTheme} (index: {index})");

        Application.Current.RequestedThemeVariant = newTheme;

        // Theme changes will be handled by components via MessageBus
        // TextMate service integration removed - handled by Editor component
        
        // Broadcast theme change to all subscribers (for UI updates, etc.)
        MessageBus.Current.SendMessage(new ThemeChangedMessage(newTheme));
        
        Logger.Info($"[ThemeService] Theme change message sent: {newTheme}");
    }
}