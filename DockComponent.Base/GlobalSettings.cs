using System.Collections.Concurrent;
using System.ComponentModel;
using ReactiveUI;

namespace DockComponent.Base;

/// <summary>
/// Global settings accessible across all components and the main application
/// Thread-safe reactive settings store
/// </summary>
public static class GlobalSettings
{
    private static readonly ConcurrentDictionary<string, object> _settings = new();
    
    // Theme Settings
    public static bool IsDarkTheme
    {
        get => GetSetting<bool>("Theme.IsDarkTheme", true);
        set => SetSetting("Theme.IsDarkTheme", value);
    }
    
    public static int ThemeIndex
    {
        get => GetSetting<int>("Theme.ThemeIndex", 1);
        set => SetSetting("Theme.ThemeIndex", value);
    }
    
    // UI Settings  
    public static bool EnableAcrylic
    {
        get => GetSetting<bool>("UI.EnableAcrylic", true);
        set => SetSetting("UI.EnableAcrylic", value);
    }
    
    public static bool EnableAnimations
    {
        get => GetSetting<bool>("UI.EnableAnimations", true);
        set => SetSetting("UI.EnableAnimations", value);
    }
    
    // Dock Layout Settings
    public static int SavedLayoutVersion
    {
        get => GetSetting<int>("DockLayout.SavedLayoutVersion", 1);
        set => SetSetting("DockLayout.SavedLayoutVersion", value);
    }
    
    /// <summary>
    /// Generic setting getter with default value
    /// </summary>
    public static T GetSetting<T>(string key, T defaultValue = default(T))
    {
        if (_settings.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }
    
    /// <summary>
    /// Generic setting setter with change notification
    /// </summary>
    public static void SetSetting<T>(string key, T value)
    {
        var oldValue = GetSetting<T>(key);
        _settings.AddOrUpdate(key, value, (k, v) => value);
        
        if (!Equals(oldValue, value))
        {
            SettingChanged?.Invoke(key, oldValue, value);
        }
    }
    
    /// <summary>
    /// Event fired when any setting changes
    /// Args: (key, oldValue, newValue)
    /// </summary>
    public static event Action<string, object?, object?>? SettingChanged;
    
    /// <summary>
    /// Bulk update settings from dictionary (for loading from JSON)
    /// </summary>
    public static void LoadFromDictionary(Dictionary<string, object> settings)
    {
        foreach (var kvp in settings)
        {
            _settings.AddOrUpdate(kvp.Key, kvp.Value, (k, v) => kvp.Value);
        }
        SettingChanged?.Invoke("*", null, null); // Bulk change notification
    }
    
    /// <summary>
    /// Export all settings to dictionary (for saving to JSON)
    /// </summary>
    public static Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>(_settings);
    }
    
    /// <summary>
    /// Clear all settings (for testing)
    /// </summary>
    public static void Clear()
    {
        _settings.Clear();
        SettingChanged?.Invoke("*", null, null);
    }
}