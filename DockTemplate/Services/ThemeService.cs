using System;
using Avalonia;
using Avalonia.Styling;
using ReactiveUI;
using DockTemplate.Messages;
using NLog;

namespace DockTemplate.Services;

public class ThemeService : IThemeService
{
    private readonly TextMateService? _textMateService;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public ThemeService(TextMateService? textMateService = null)
    {
        _textMateService = textMateService;
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

        // Update TextMate service directly for immediate response
        _textMateService?.UpdateTheme(newTheme);

        // Broadcast theme change to all subscribers (for UI updates, etc.)
        MessageBus.Current.SendMessage(new ThemeChangedMessage(newTheme));
        
        Logger.Info($"[ThemeService] Theme change message sent: {newTheme}");
    }
}