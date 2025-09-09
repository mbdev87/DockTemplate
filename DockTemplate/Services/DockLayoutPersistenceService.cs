using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dock.Model.Core;
using Dock.Model.Controls;
using DockTemplate.Models;
using DockTemplate.ViewModels;
using DockComponent.Base;
using NLog;

namespace DockTemplate.Services;

public class DockLayoutPersistenceService : IDockLayoutPersistence
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    private readonly DockFactory _dockFactory;
    private readonly ISettingsService _settingsService;

    public DockLayoutPersistenceService(DockFactory dockFactory, ISettingsService settingsService)
    {
        _dockFactory = dockFactory;
        _settingsService = settingsService;
    }

    public DockLayoutData CaptureLayout()
    {
        try
        {
            Logger.Info("🔄 Capturing current dock layout...");
            
            var layoutData = new DockLayoutData();
            var registry = ComponentRegistry.Instance;
            
            // Capture Left Dock
            if (_dockFactory.LeftDock != null)
            {
                var leftContainer = CaptureContainer("Left", "Left", _dockFactory.LeftDock);
                if (leftContainer.Components.Any())
                {
                    layoutData.Containers.Add(leftContainer);
                }
            }
            
            // Capture Right Dock
            if (_dockFactory.RightDock != null)
            {
                var rightContainer = CaptureContainer("Right", "Right", _dockFactory.RightDock);
                if (rightContainer.Components.Any())
                {
                    layoutData.Containers.Add(rightContainer);
                }
            }
            
            // Capture Bottom Dock
            if (_dockFactory.BottomDock != null)
            {
                var bottomContainer = CaptureContainer("Bottom", "Bottom", _dockFactory.BottomDock);
                if (bottomContainer.Components.Any())
                {
                    layoutData.Containers.Add(bottomContainer);
                }
            }
            
            // Capture Document Dock
            if (_dockFactory.DocumentDock != null)
            {
                var documentContainer = CaptureDocumentContainer();
                if (documentContainer.Components.Any())
                {
                    layoutData.Containers.Add(documentContainer);
                    layoutData.ActiveDocument = _dockFactory.DocumentDock.ActiveDockable?.Id;
                }
            }
            
            Logger.Info($"🔄 Layout captured: {layoutData.Containers.Count} containers, {layoutData.Containers.Sum(c => c.Components.Count)} components");
            return layoutData;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to capture dock layout");
            return new DockLayoutData();
        }
    }

    private DockContainerData CaptureContainer(string id, string position, IDock dock)
    {
        var container = new DockContainerData
        {
            Id = id,
            Position = position,
            Proportion = dock.Proportion,
            IsCollapsed = !dock.IsActive,
            IsVisible = true, // Assume visible if dock exists
            ActiveComponent = dock.ActiveDockable?.Id
        };

        if (dock.VisibleDockables != null)
        {
            var order = 0;
            foreach (var dockable in dock.VisibleDockables)
            {
                if (dockable != null && !string.IsNullOrEmpty(dockable.Id))
                {
                    var componentData = new DockComponentData
                    {
                        Id = dockable.Id,
                        Type = "Tool",
                        Title = dockable.Title ?? dockable.Id,
                        IsVisible = true, // Assume visible if in VisibleDockables collection
                        Order = order++,
                        IsAvailable = true
                    };
                    
                    // Try to find the component instance ID from registry
                    var registeredComponent = ComponentRegistry.Instance.ComponentTools
                        .FirstOrDefault(ct => ct.Id == dockable.Id);
                    if (registeredComponent != null)
                    {
                        componentData.ComponentInstanceId = registeredComponent.ComponentInstanceId.ToString();
                    }
                    
                    container.Components.Add(componentData);
                }
            }
        }

        return container;
    }

    private DockContainerData CaptureDocumentContainer()
    {
        var container = new DockContainerData
        {
            Id = "Documents",
            Position = "Document",
            Proportion = 1.0,
            IsCollapsed = false,
            IsVisible = true,
            ActiveComponent = _dockFactory.DocumentDock?.ActiveDockable?.Id
        };

        if (_dockFactory.DocumentDock?.VisibleDockables != null)
        {
            var order = 0;
            foreach (var dockable in _dockFactory.DocumentDock.VisibleDockables)
            {
                if (dockable != null && !string.IsNullOrEmpty(dockable.Id))
                {
                    var componentData = new DockComponentData
                    {
                        Id = dockable.Id,
                        Type = "Document",
                        Title = dockable.Title ?? dockable.Id,
                        IsVisible = true, // Assume visible if in VisibleDockables collection
                        Order = order++,
                        IsAvailable = true
                    };

                    // For documents, try to capture file path and cursor position
                    if (TryGetDocumentInfo(dockable, out var filePath, out var line, out var column))
                    {
                        componentData.FilePath = filePath;
                        componentData.CursorLine = line;
                        componentData.CursorColumn = column;
                    }

                    container.Components.Add(componentData);
                }
            }
        }

        return container;
    }

    private bool TryGetDocumentInfo(IDockable dockable, out string? filePath, out int? line, out int? column)
    {
        filePath = null;
        line = null;
        column = null;

        try
        {
            // Try to get document info via reflection (duck typing)
            var type = dockable.GetType();
            
            var filePathProp = type.GetProperty("FilePath");
            if (filePathProp != null)
            {
                filePath = filePathProp.GetValue(dockable) as string;
            }
            
            var lineProp = type.GetProperty("CurrentLine");
            if (lineProp != null && int.TryParse(lineProp.GetValue(dockable)?.ToString(), out var currentLine))
            {
                line = currentLine;
            }
            
            var columnProp = type.GetProperty("CurrentColumn");
            if (columnProp != null && int.TryParse(columnProp.GetValue(dockable)?.ToString(), out var currentColumn))
            {
                column = currentColumn;
            }

            return !string.IsNullOrEmpty(filePath);
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, $"Could not extract document info from {dockable.GetType().Name}");
            return false;
        }
    }

    public async Task<bool> RestoreLayoutAsync(DockLayoutData layoutData)
    {
        try
        {
            Logger.Info($"🔄 Restoring dock layout (version {layoutData.Version})...");
            
            var missingComponents = GetMissingComponents(layoutData);
            if (missingComponents.Any())
            {
                Logger.Warn($"🔄 {missingComponents.Count} components are no longer available: {string.Join(", ", missingComponents)}");
            }

            var registry = ComponentRegistry.Instance;
            var availableTools = registry.ComponentTools.ToDictionary(t => t.Id, t => t);
            var availableDocuments = registry.ComponentDocuments.ToDictionary(d => d.Id, d => d);

            // Restore each container
            foreach (var containerData in layoutData.Containers)
            {
                await RestoreContainer(containerData, availableTools, availableDocuments);
            }

            // Restore active document
            if (!string.IsNullOrEmpty(layoutData.ActiveDocument) && _dockFactory.DocumentDock != null)
            {
                var activeDoc = _dockFactory.DocumentDock.VisibleDockables?
                    .FirstOrDefault(d => d.Id == layoutData.ActiveDocument);
                if (activeDoc != null)
                {
                    _dockFactory.DocumentDock.ActiveDockable = activeDoc;
                }
            }

            Logger.Info("🔄 Dock layout restoration completed");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to restore dock layout");
            return false;
        }
    }

    private async Task RestoreContainer(DockContainerData containerData, 
        Dictionary<string, ComponentRegistration> availableTools,
        Dictionary<string, ComponentRegistration> availableDocuments)
    {
        IDock? targetDock = containerData.Position switch
        {
            "Left" => _dockFactory.LeftDock,
            "Right" => _dockFactory.RightDock,
            "Bottom" => _dockFactory.BottomDock,
            "Document" => _dockFactory.DocumentDock,
            _ => null
        };

        if (targetDock == null) return;

        // Clear existing dockables
        if (targetDock.VisibleDockables != null)
        {
            targetDock.VisibleDockables = _dockFactory.CreateList<IDockable>();
        }

        // Restore components in order
        var dockables = new List<IDockable>();
        foreach (var componentData in containerData.Components.OrderBy(c => c.Order))
        {
            if (componentData.Type == "Tool" && availableTools.TryGetValue(componentData.Id, out var tool))
            {
                if (tool.ViewModel is IDockable toolDockable)
                {
                    dockables.Add(toolDockable);
                    Logger.Debug($"🔄 Restored tool: {componentData.Id}");
                }
            }
            else if (componentData.Type == "Document" && availableDocuments.TryGetValue(componentData.Id, out var doc))
            {
                if (doc.ViewModel is IDockable docDockable)
                {
                    dockables.Add(docDockable);
                    Logger.Debug($"🔄 Restored document: {componentData.Id}");
                }
            }
            else if (componentData.Type == "Document" && !string.IsNullOrEmpty(componentData.FilePath))
            {
                // Try to restore file document
                try
                {
                    _dockFactory.OpenDocument(componentData.FilePath, componentData.CursorLine);
                    Logger.Debug($"🔄 Restored file document: {componentData.FilePath}");
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, $"Could not restore document: {componentData.FilePath}");
                }
            }
            else
            {
                Logger.Debug($"🔄 Skipped unavailable component: {componentData.Id}");
            }
        }

        if (dockables.Any())
        {
            targetDock.VisibleDockables = _dockFactory.CreateList(dockables.ToArray());
            
            // Restore active component
            if (!string.IsNullOrEmpty(containerData.ActiveComponent))
            {
                var activeDockable = dockables.FirstOrDefault(d => d.Id == containerData.ActiveComponent);
                if (activeDockable != null)
                {
                    targetDock.ActiveDockable = activeDockable;
                }
            }
        }

        // Restore container properties
        targetDock.Proportion = containerData.Proportion;
        targetDock.IsActive = !containerData.IsCollapsed;
        // Note: IsVisible property not available in this dock library version
    }

    public List<string> GetMissingComponents(DockLayoutData layoutData)
    {
        var missing = new List<string>();
        var registry = ComponentRegistry.Instance;
        var availableIds = registry.ComponentTools.Select(t => t.Id)
            .Concat(registry.ComponentDocuments.Select(d => d.Id))
            .ToHashSet();

        foreach (var container in layoutData.Containers)
        {
            foreach (var component in container.Components)
            {
                if (component.Type == "Tool" || component.Type == "Document")
                {
                    if (!availableIds.Contains(component.Id))
                    {
                        missing.Add(component.Id);
                    }
                }
            }
        }

        return missing.Distinct().ToList();
    }

    public async Task SaveCurrentLayoutAsync()
    {
        try
        {
            var layoutData = CaptureLayout();
            
            // Store in global settings
            DockComponent.Base.GlobalSettings.SetSetting("DockLayout.LayoutData", layoutData);
            DockComponent.Base.GlobalSettings.SavedLayoutVersion++;
            
            // Also save to persistent storage
            await _settingsService.SaveSettingsAsync();
            
            Logger.Info($"🔄 Layout saved (version {DockComponent.Base.GlobalSettings.SavedLayoutVersion})");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to save dock layout");
        }
    }

    public async Task<bool> LoadLayoutAsync()
    {
        try
        {
            var layoutData = DockComponent.Base.GlobalSettings.GetSetting<DockLayoutData>("DockLayout.LayoutData");
            if (layoutData != null)
            {
                Logger.Info($"🔄 Loading saved layout (version {layoutData.Version})");
                return await RestoreLayoutAsync(layoutData);
            }
            
            Logger.Info("🔄 No saved layout found");
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load dock layout");
            return false;
        }
    }
}