using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DockTemplate.Models;

public class AppSettings
{
    [JsonPropertyName("theme")]
    public ThemeSettings Theme { get; set; } = new();
    
    [JsonPropertyName("ui")]
    public UISettings UI { get; set; } = new();
    
    [JsonPropertyName("dockLayout")]
    public DockLayoutSettings DockLayout { get; set; } = new();
}

public class ThemeSettings
{
    [JsonPropertyName("isDarkTheme")]
    public bool IsDarkTheme { get; set; } = true;
    
    [JsonPropertyName("themeIndex")]
    public int ThemeIndex { get; set; } = 1; // 0 = Light, 1 = Dark
}

public class UISettings
{
    [JsonPropertyName("enableAcrylic")]
    public bool EnableAcrylic { get; set; } = true;
    
    [JsonPropertyName("enableAnimations")]
    public bool EnableAnimations { get; set; } = true;
}

public class DockLayoutSettings
{
    [JsonPropertyName("savedLayoutVersion")]
    public int SavedLayoutVersion { get; set; } = 1;
    
    [JsonPropertyName("componentPositions")]
    public Dictionary<string, ComponentPosition> ComponentPositions { get; set; } = new();
    
    [JsonPropertyName("removedComponents")]
    public List<string> RemovedComponents { get; set; } = new();
}

public class ComponentPosition
{
    [JsonPropertyName("componentId")]
    public string ComponentId { get; set; } = string.Empty;
    
    [JsonPropertyName("dockPosition")]
    public string DockPosition { get; set; } = string.Empty; // "Left", "Right", "Bottom", "Document", etc.
    
    [JsonPropertyName("isVisible")]
    public bool IsVisible { get; set; } = true;
    
    [JsonPropertyName("width")]
    public double? Width { get; set; }
    
    [JsonPropertyName("height")]
    public double? Height { get; set; }
    
    [JsonPropertyName("order")]
    public int Order { get; set; } = 0;
}