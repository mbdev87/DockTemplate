using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DockTemplate.Models;
using NLog;

namespace DockTemplate.Services;

public class DockLayoutService : IDockLayoutService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    private readonly ISettingsService _settingsService;
    private readonly Dictionary<string, string> _availableComponents = new();

    public DockLayoutService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task SaveCurrentLayoutAsync()
    {
        try
        {
            Logger.Info("Saving current dock layout...");
            
            // For now, we'll increment the version to indicate layout was saved
            _settingsService.Settings.DockLayout.SavedLayoutVersion++;
            
            await _settingsService.SaveSettingsAsync();
            Logger.Info($"Dock layout saved (version {_settingsService.Settings.DockLayout.SavedLayoutVersion})");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to save dock layout");
        }
    }

    public async Task RestoreLayoutAsync()
    {
        try
        {
            Logger.Info("Restoring dock layout from settings...");
            
            await _settingsService.LoadSettingsAsync();
            
            var layoutSettings = _settingsService.Settings.DockLayout;
            Logger.Info($"Layout version: {layoutSettings.SavedLayoutVersion}");
            Logger.Info($"Saved component positions: {layoutSettings.ComponentPositions.Count}");
            Logger.Info($"Removed components: {layoutSettings.RemovedComponents.Count}");
            
            // Log which components we're aware of vs what's saved
            foreach (var componentId in _availableComponents.Keys)
            {
                if (layoutSettings.ComponentPositions.ContainsKey(componentId))
                {
                    var position = layoutSettings.ComponentPositions[componentId];
                    Logger.Info($"  Component '{componentId}' -> {position.DockPosition} (visible: {position.IsVisible})");
                }
                else if (layoutSettings.RemovedComponents.Contains(componentId))
                {
                    Logger.Info($"  Component '{componentId}' -> REMOVED");
                }
                else
                {
                    Logger.Info($"  Component '{componentId}' -> NEW (no saved position)");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to restore dock layout");
        }
    }

    public void RegisterComponent(string componentId, string displayName, string dockPosition = "Left")
    {
        _availableComponents[componentId] = displayName;
        Logger.Info($"Registered component: {componentId} ({displayName}) -> default position: {dockPosition}");
        
        // If this component doesn't have a saved position and isn't removed, create default position
        var position = _settingsService.GetComponentPosition(componentId);
        if (position == null && !_settingsService.IsComponentRemoved(componentId))
        {
            _settingsService.SaveComponentPosition(componentId, dockPosition, true);
            Logger.Debug($"Created default position for new component: {componentId}");
        }
    }

    public void UnregisterComponent(string componentId)
    {
        if (_availableComponents.ContainsKey(componentId))
        {
            _availableComponents.Remove(componentId);
            _settingsService.MarkComponentRemoved(componentId);
            Logger.Info($"Unregistered component: {componentId} (marked as removed)");
        }
    }

    public bool ShouldRestoreComponent(string componentId)
    {
        // Don't restore if component was explicitly removed
        if (_settingsService.IsComponentRemoved(componentId))
        {
            Logger.Debug($"Component {componentId} should NOT be restored (marked as removed)");
            return false;
        }
        
        // Restore if we have position data or if it's a new component
        var position = _settingsService.GetComponentPosition(componentId);
        var shouldRestore = position?.IsVisible ?? true; // Default to visible for new components
        
        Logger.Debug($"Component {componentId} should {(shouldRestore ? "" : "NOT ")}be restored");
        return shouldRestore;
    }

    public ComponentPosition? GetComponentRestorePosition(string componentId)
    {
        return _settingsService.GetComponentPosition(componentId);
    }

    public List<string> GetAvailableComponents()
    {
        return _availableComponents.Keys.ToList();
    }

    public List<string> GetRemovedComponents()
    {
        return _settingsService.Settings.DockLayout.RemovedComponents.ToList();
    }
}