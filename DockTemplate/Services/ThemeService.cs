using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using ReactiveUI;
using DockTemplate.Messages;
using DockTemplate.Models;
using NLog;

namespace DockTemplate.Services;

public class ThemeService : IThemeService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly ISettingsService _settingsService;

    public ThemeService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task InitializeFromSettingsAsync()
    {
        try
        {
            await _settingsService.LoadSettingsAsync();
            var savedThemeIndex = _settingsService.Settings.Theme.ThemeIndex;
            
            Logger.Info($"[ThemeService] Initializing theme from settings: index {savedThemeIndex}");
            
            // Apply the saved theme without saving again (to avoid recursion)
            // CRITICAL: Must dispatch to UI thread for cross-platform compatibility
            await Dispatcher.UIThread.InvokeAsync(() => ApplyThemeWithoutSaving(savedThemeIndex));
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize theme from settings, using default");
            await Dispatcher.UIThread.InvokeAsync(() => ApplyThemeWithoutSaving(1)); // Default to dark theme
        }
    }

    private void ApplyThemeWithoutSaving(int index)
    {
        if (Application.Current is null)
        {
            return;
        }

        var newTheme = index switch
        {
            0 => ThemeVariant.Light,
            1 => ThemeVariant.Dark,
            _ => ThemeVariant.Dark
        };

        Application.Current.RequestedThemeVariant = newTheme;
        MessageBus.Current.SendMessage(new ThemeChangedMessage(newTheme));
        
        Logger.Info($"[ThemeService] Applied theme without saving: {newTheme}");
    }

    public void InitializeFromSettingsSync()
    {
        try
        {
            // Load settings file directly - no async needed for startup
            var settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "DockTemplate", 
                "settings.json");
                
            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    // Load into global settings
                    DockComponent.Base.GlobalSettings.IsDarkTheme = settings.Theme.IsDarkTheme;
                    DockComponent.Base.GlobalSettings.ThemeIndex = settings.Theme.ThemeIndex;
                    DockComponent.Base.GlobalSettings.EnableAcrylic = settings.UI.EnableAcrylic;
                    DockComponent.Base.GlobalSettings.EnableAnimations = settings.UI.EnableAnimations;
                    
                    var savedThemeIndex = settings.Theme.ThemeIndex;
                    Logger.Info($"[ThemeService] Loading theme SYNC from settings: index {savedThemeIndex}");
                    Logger.Info($"[ThemeService] Global settings populated - Acrylic: {DockComponent.Base.GlobalSettings.EnableAcrylic}");
                    ApplyThemeWithoutSaving(savedThemeIndex);
                    return;
                }
            }
            
            Logger.Info("[ThemeService] No settings file found, using default dark theme");
            ApplyThemeWithoutSaving(1); // Default to dark theme
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load theme from settings, using default");
            ApplyThemeWithoutSaving(1); // Default to dark theme
        }
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

        // Ensure UI changes happen on UI thread
        Dispatcher.UIThread.Post(() =>
        {
            Application.Current.RequestedThemeVariant = newTheme;
            MessageBus.Current.SendMessage(new ThemeChangedMessage(newTheme));
        });

        // Save theme preference to settings (can be done on background thread)
        _settingsService.SetTheme(newTheme == ThemeVariant.Dark);
        _ = Task.Run(async () => await _settingsService.SaveSettingsAsync());
        
        Logger.Info($"[ThemeService] Theme change message sent: {newTheme}");
    }
}