using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DockTemplate.Models;
using NLog;

namespace DockTemplate.Services;

public class SettingsService : ISettingsService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private const string SettingsFileName = "settings.json";
    
    private readonly string _settingsDirectory;
    private readonly string _settingsFilePath;
    
    public AppSettings Settings { get; private set; } = new();

    public SettingsService()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _settingsDirectory = Path.Combine(localAppData, "DockTemplate");
        _settingsFilePath = Path.Combine(_settingsDirectory, SettingsFileName);
        
        Logger.Info($"Settings will be stored at: {_settingsFilePath}");
        EnsureSettingsDirectoryExists();
    }

    public async Task LoadSettingsAsync()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                Logger.Info("Settings file does not exist, using defaults");
                Settings = new AppSettings();
                await SaveSettingsAsync(); // Create default settings file
                return;
            }

            var json = await File.ReadAllTextAsync(_settingsFilePath);
            var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json);
            
            if (loadedSettings != null)
            {
                Settings = loadedSettings;
                Logger.Info("Settings loaded successfully");
                Logger.Debug($"Loaded settings: Theme={Settings.Theme.IsDarkTheme}, Acrylic={Settings.UI.EnableAcrylic}");
            }
            else
            {
                Logger.Warn("Settings file was empty or invalid, using defaults");
                Settings = new AppSettings();
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load settings, using defaults");
            Settings = new AppSettings();
        }
    }

    public async Task SaveSettingsAsync()
    {
        try
        {
            EnsureSettingsDirectoryExists();
            
            // Update layout data from global settings before saving
            var layoutData = DockComponent.Base.GlobalSettings.GetSetting<DockLayoutData>("DockLayout.LayoutData");
            if (layoutData != null)
            {
                Settings.DockLayout.LayoutData = layoutData;
            }
            Settings.DockLayout.SavedLayoutVersion = DockComponent.Base.GlobalSettings.SavedLayoutVersion;
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var json = JsonSerializer.Serialize(Settings, options);
            await File.WriteAllTextAsync(_settingsFilePath, json);
            
            Logger.Info("Settings saved successfully");
            Logger.Debug($"Saved settings: Theme={Settings.Theme.IsDarkTheme}, Acrylic={Settings.UI.EnableAcrylic}, Layout={layoutData?.Version}");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to save settings");
        }
    }

    public void SetTheme(bool isDarkTheme)
    {
        Settings.Theme.IsDarkTheme = isDarkTheme;
        Settings.Theme.ThemeIndex = isDarkTheme ? 1 : 0;
        Logger.Info($"Theme setting updated: {(isDarkTheme ? "Dark" : "Light")}");
    }

    public void SetAcrylic(bool enableAcrylic)
    {
        Settings.UI.EnableAcrylic = enableAcrylic;
        Logger.Info($"Acrylic setting updated: {enableAcrylic}");
    }

    public void SaveComponentPosition(string componentId, string dockPosition, bool isVisible, double? width = null, double? height = null, int order = 0)
    {
        Settings.DockLayout.ComponentPositions[componentId] = new ComponentPosition
        {
            ComponentId = componentId,
            DockPosition = dockPosition,
            IsVisible = isVisible,
            Width = width,
            Height = height,
            Order = order
        };
        
        Logger.Debug($"Component position saved: {componentId} -> {dockPosition} (visible: {isVisible})");
    }

    public void MarkComponentRemoved(string componentId)
    {
        if (!Settings.DockLayout.RemovedComponents.Contains(componentId))
        {
            Settings.DockLayout.RemovedComponents.Add(componentId);
            Logger.Info($"Component marked as removed: {componentId}");
        }
        
        // Also remove from positions since it's no longer available
        if (Settings.DockLayout.ComponentPositions.ContainsKey(componentId))
        {
            Settings.DockLayout.ComponentPositions.Remove(componentId);
            Logger.Debug($"Removed position data for removed component: {componentId}");
        }
    }

    public ComponentPosition? GetComponentPosition(string componentId)
    {
        return Settings.DockLayout.ComponentPositions.TryGetValue(componentId, out var position) ? position : null;
    }

    public bool IsComponentRemoved(string componentId)
    {
        return Settings.DockLayout.RemovedComponents.Contains(componentId);
    }

    private void EnsureSettingsDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(_settingsDirectory))
            {
                Directory.CreateDirectory(_settingsDirectory);
                Logger.Info($"Created settings directory: {_settingsDirectory}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to create settings directory");
        }
    }
}