using System;
using System.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Dock.Model.Core;
using NLog;
using DockComponent.Base;

namespace DockTemplate.Services;

public class AcrylicLayoutManager : ReactiveObject
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    [Reactive] public bool IsAcrylicLayoutActive { get; set; } = true; // Start in acrylic mode
    [Reactive] public IDockable? AcrylicSidebarTool { get; set; }
    
    private readonly ComponentRegistry _componentRegistry;
    private readonly ISettingsService? _settingsService;
    
    public AcrylicLayoutManager(ISettingsService? settingsService = null)
    {
        _componentRegistry = ComponentRegistry.Instance;
        _settingsService = settingsService;
    }
    
    /// <summary>
    /// Initialize acrylic setting early during app startup (synchronous)
    /// </summary>
    public void InitializeAcrylicModeEarly()
    {
        try
        {
            // Use global settings (already loaded by ThemeService)
            IsAcrylicLayoutActive = DockComponent.Base.GlobalSettings.EnableAcrylic;
            _isEarlyInitialized = true;
            Logger.Info($"🎨 Loaded acrylic setting EARLY from global settings: {IsAcrylicLayoutActive}");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load acrylic setting, using default");
            IsAcrylicLayoutActive = true; // Default to enabled
            _isEarlyInitialized = true;
        }
    }

    private bool _isEarlyInitialized = false;

    /// <summary>
    /// Initialize acrylic mode after components are loaded
    /// </summary>
    public void InitializeAcrylicMode()
    {
        // Only load from settings service if we didn't already load early
        if (!_isEarlyInitialized && _settingsService != null)
        {
            IsAcrylicLayoutActive = _settingsService.Settings.UI.EnableAcrylic;
            Logger.Info($"🎨 Loaded acrylic setting from config: {IsAcrylicLayoutActive}");
        }
        else if (_isEarlyInitialized)
        {
            Logger.Info($"🎨 Acrylic setting already loaded early: {IsAcrylicLayoutActive}");
        }
        
        if (IsAcrylicLayoutActive)
        {
            var primaryLeftTool = GetPrimaryLeftTool();
            if (primaryLeftTool != null)
            {
                AcrylicSidebarTool = primaryLeftTool;
                Logger.Info($"🎨 Initialized acrylic mode with sidebar tool: {primaryLeftTool.Title}");
            }
        }
    }
    
    /// <summary>
    /// Toggles between normal and acrylic layout modes
    /// </summary>
    public void ToggleAcrylicLayout()
    {
        if (IsAcrylicLayoutActive)
        {
            DisableAcrylicLayout();
        }
        else
        {
            EnableAcrylicLayout();
        }
        
        // Save setting
        SaveAcrylicSetting();
    }
    
    /// <summary>
    /// Enables acrylic layout with primary left tool in sidebar
    /// </summary>
    public void EnableAcrylicLayout()
    {
        var primaryLeftTool = GetPrimaryLeftTool();
        if (primaryLeftTool == null)
        {
            Logger.Warn("No left tools available for acrylic layout - showing empty sidebar");
        }
        
        AcrylicSidebarTool = primaryLeftTool;
        IsAcrylicLayoutActive = true;
        
        Logger.Info($"🎨 Acrylic layout enabled with sidebar tool: {primaryLeftTool?.Title ?? "None"}");
    }
    
    /// <summary>
    /// Disables acrylic layout and returns to normal dock mode
    /// </summary>
    public void DisableAcrylicLayout()
    {
        AcrylicSidebarTool = null;
        IsAcrylicLayoutActive = false;
        
        Logger.Info("🎨 Acrylic layout disabled - returned to normal dock mode");
    }
    
    /// <summary>
    /// Gets the primary left tool for acrylic sidebar
    /// Priority: Primary marked tool > First left tool > null
    /// </summary>
    private IDockable? GetPrimaryLeftTool()
    {
        var leftTools = _componentRegistry.ComponentTools
            .Where(t => t.Position == DockPosition.Left)
            .ToList();
            
        if (!leftTools.Any())
        {
            Logger.Info("No left tools found for acrylic layout");
            return null;
        }
        
        // First priority: tool marked as primary
        var primaryTool = leftTools.FirstOrDefault(t => t.IsPrimary);
        if (primaryTool?.ViewModel is IDockable primaryDockable)
        {
            Logger.Info($"Found primary left tool for acrylic sidebar: {primaryTool.Id}");
            return primaryDockable;
        }
        
        // Second priority: first available left tool
        var firstTool = leftTools.FirstOrDefault();
        if (firstTool?.ViewModel is IDockable firstDockable)
        {
            Logger.Info($"Using first left tool for acrylic sidebar: {firstTool.Id}");
            return firstDockable;
        }
        
        Logger.Warn("No suitable left tools found for acrylic layout");
        return null;
    }
    
    /// <summary>
    /// Checks if a tool should be shown in the acrylic sidebar
    /// </summary>
    public bool IsToolInAcrylicSidebar(IDockable tool)
    {
        return IsAcrylicLayoutActive && AcrylicSidebarTool == tool;
    }
    
    /// <summary>
    /// Gets all left tools that should NOT be in the acrylic sidebar
    /// (these go back to the normal left dock)
    /// </summary>
    public System.Collections.Generic.IEnumerable<IDockable> GetNormalLeftTools()
    {
        if (!IsAcrylicLayoutActive) yield break;
        
        var leftTools = _componentRegistry.ComponentTools
            .Where(t => t.Position == DockPosition.Left && t.ViewModel is IDockable)
            .Select(t => t.ViewModel as IDockable)
            .Where(d => d != null && d != AcrylicSidebarTool);
            
        foreach (var tool in leftTools)
        {
            yield return tool!;
        }
    }
    
    /// <summary>
    /// Saves the current acrylic setting to persistent storage
    /// </summary>
    private void SaveAcrylicSetting()
    {
        // Update global settings immediately
        DockComponent.Base.GlobalSettings.EnableAcrylic = IsAcrylicLayoutActive;
        
        if (_settingsService != null)
        {
            _settingsService.SetAcrylic(IsAcrylicLayoutActive);
            _ = Task.Run(async () => await _settingsService.SaveSettingsAsync());
            Logger.Info($"🎨 Saved acrylic setting: {IsAcrylicLayoutActive}");
        }
    }
}