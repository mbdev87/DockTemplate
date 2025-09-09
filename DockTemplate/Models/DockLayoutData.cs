using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DockTemplate.Models;

/// <summary>
/// Represents the complete dock layout for persistence
/// Handles nested dock structure with components that may come and go
/// </summary>
public class DockLayoutData
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;
    
    [JsonPropertyName("containers")]
    public List<DockContainerData> Containers { get; set; } = new();
    
    [JsonPropertyName("activeDocument")]
    public string? ActiveDocument { get; set; }
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a dock container (Left, Right, Bottom, Document areas)
/// </summary>
public class DockContainerData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("position")]
    public string Position { get; set; } = string.Empty; // "Left", "Right", "Bottom", "Document"
    
    [JsonPropertyName("proportion")]
    public double Proportion { get; set; } = 0.2;
    
    [JsonPropertyName("isCollapsed")]
    public bool IsCollapsed { get; set; } = false;
    
    [JsonPropertyName("isVisible")]
    public bool IsVisible { get; set; } = true;
    
    [JsonPropertyName("components")]
    public List<DockComponentData> Components { get; set; } = new();
    
    [JsonPropertyName("activeComponent")]
    public string? ActiveComponent { get; set; }
}

/// <summary>
/// Represents a component within a dock container
/// Handles components that might not be available (plugins removed)
/// </summary>
public class DockComponentData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "Tool" or "Document"
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("isVisible")]
    public bool IsVisible { get; set; } = true;
    
    [JsonPropertyName("order")]
    public int Order { get; set; } = 0;
    
    [JsonPropertyName("componentInstanceId")]
    public string? ComponentInstanceId { get; set; }
    
    [JsonPropertyName("isAvailable")]
    public bool IsAvailable { get; set; } = true; // False if plugin was removed
    
    // Document-specific properties
    [JsonPropertyName("filePath")]
    public string? FilePath { get; set; }
    
    [JsonPropertyName("cursorLine")]
    public int? CursorLine { get; set; }
    
    [JsonPropertyName("cursorColumn")]
    public int? CursorColumn { get; set; }
}

/// <summary>
/// Layout persistence service that handles saving/loading dock arrangements
/// Gracefully handles missing plugins and component changes
/// </summary>
public interface IDockLayoutPersistence
{
    /// <summary>
    /// Capture current dock layout state
    /// </summary>
    DockLayoutData CaptureLayout();
    
    /// <summary>
    /// Restore dock layout, skipping unavailable components
    /// </summary>
    Task<bool> RestoreLayoutAsync(DockLayoutData layoutData);
    
    /// <summary>
    /// Save current layout to global settings
    /// </summary>
    Task SaveCurrentLayoutAsync();
    
    /// <summary>
    /// Load layout from global settings and restore
    /// </summary>
    Task<bool> LoadLayoutAsync();
    
    /// <summary>
    /// Get list of components that were saved but are no longer available
    /// </summary>
    List<string> GetMissingComponents(DockLayoutData layoutData);
}