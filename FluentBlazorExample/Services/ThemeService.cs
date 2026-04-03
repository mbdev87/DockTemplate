using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.AspNetCore.Components;

namespace FluentBlazorExample.Services;

public interface IThemeService
{
    DesignThemeModes Mode { get; set; }
    OfficeColor OfficeColor { get; set; }
    event Action? ThemeChanged;
    void ToggleTheme();
}

public class ThemeService : IThemeService
{
    private DesignThemeModes _mode = DesignThemeModes.System;
    private OfficeColor _officeColor = OfficeColor.Word;

    public DesignThemeModes Mode
    {
        get => _mode;
        set
        {
            if (_mode != value)
            {
                var oldValue = _mode;
                _mode = value;
                Console.WriteLine(
                    $"[THEME] ThemeService: Mode changed from {oldValue} to {_mode}");

                try
                {
                    ThemeChanged?.Invoke();
                    Console.WriteLine(
                        $"[THEME] ThemeService: ThemeChanged event fired");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"[THEME] ThemeService: Error firing ThemeChanged event: {ex.Message}");
                }
            }
        }
    }

    public OfficeColor OfficeColor
    {
        get => _officeColor;
        set
        {
            if (_officeColor != value)
            {
                _officeColor = value;
                Console.WriteLine(
                    $"ThemeService: OfficeColor changed to {_officeColor}");
                ThemeChanged?.Invoke();
            }
        }
    }

    // Add EventCallback properties to support binding
    public EventCallback<DesignThemeModes> ModeChanged { get; set; }
    public EventCallback<OfficeColor> OfficeColorChanged { get; set; }

    public event Action? ThemeChanged;

    public void ToggleTheme()
    {
        var currentMode = Mode;
        Console.WriteLine(
            $"[THEME] ThemeService: ToggleTheme() called, current mode: {currentMode}");

        var newMode = currentMode switch
        {
            DesignThemeModes.System => DesignThemeModes.Light,
            DesignThemeModes.Light => DesignThemeModes.Dark,
            DesignThemeModes.Dark => DesignThemeModes.System,
            _ => DesignThemeModes.System
        };

        Console.WriteLine(
            $"[THEME] ThemeService: Setting mode from {currentMode} to {newMode}");
        Mode = newMode;
        Console.WriteLine(
            $"[THEME] ThemeService: ToggleTheme() completed, final mode: {Mode}");
    }
}