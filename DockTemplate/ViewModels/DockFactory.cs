using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using DockComponent.Base;
//using DockComponent.Editor.ViewModels.Documents; // Moved to Editor component
using DockComponent.Editor.ViewModels;
using DockComponent.SolutionExplorer.ViewModels;
using DockComponent.Output.ViewModels;
using DockComponent.ErrorList.ViewModels;
//using DockTemplate.Models.Documents; // Removed
using DockTemplate.Models.Tools;
using DockTemplate.Models;
using DockTemplate.Services;
using DockTemplate.Messages;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using ReactiveUI;
using NLog;

namespace DockTemplate.ViewModels;

public class DockFactory : Factory
{
    private IRootDock? _rootDock;
    private IDocumentDock? _documentDock;
    private IProportionalDock? _leftDock;
    private IProportionalDock? _rightDock;
    private IToolDock? _bottomDock;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    // Note: Component registrations are now stored in the singleton ComponentRegistry
    // This factory creates MINIMAL layout - components will add themselves dynamically

    public DockFactory()
    {
        
        // Listen for UI loaded message to integrate components after full initialization
        MessageBus.Current.Listen<UILoadedMessage>()
            .Subscribe(_ =>
            {
                Logger.Info("[DockFactory] Received UILoadedMessage - integrating components");
                IntegrateComponentsAfterUILoad();
            });
    }

    public override IRootDock CreateLayout()
    {
        // Create completely minimal layout - NO components loaded yet
        // Components will register themselves dynamically through the plugin system
        
        Logger.Info("[DockFactory] Creating minimal empty layout - components will populate it");

        // Create empty docks that components can populate
        var leftDock = new ProportionalDock
        {
            Proportion = 0.2,
            Orientation = Orientation.Vertical,
            ActiveDockable = new DockDock(),
            VisibleDockables = CreateList<IDockable>(),
            
        };

        var rightDock = new ProportionalDock
        {
            Proportion = 0.2,
            Orientation = Orientation.Vertical,
            ActiveDockable = new DockDock(),
            VisibleDockables = CreateList<IDockable>(),
        };

        var bottomDock = new ToolDock
        {
            Proportion = 0.2,
            ActiveDockable = new DockDock(),
            VisibleDockables = CreateList<IDockable>(),
            Alignment = Alignment.Bottom,
            IsCollapsable = true,
            IsActive = true,
            CanCloseLastDockable = false
        };

        var documentDock = new DocumentDock
        {
            IsCollapsable = false,
            ActiveDockable = new DockDock(),
            VisibleDockables = CreateList<IDockable>(),
            CanCreateDocument = true,
        };

        _documentDock = documentDock;
        
        _leftDock = leftDock;
        _rightDock = rightDock;
        _bottomDock = bottomDock;
        var mainLayout = new ProportionalDock
        {
            Orientation = Orientation.Vertical,
            VisibleDockables = CreateList<IDockable>
            (
                new ProportionalDock
                {
                    Orientation = Orientation.Horizontal,
                    VisibleDockables = CreateList<IDockable>
                    (
                            _leftDock,
                        new ProportionalDockSplitter(),
                        _documentDock,
                        new ProportionalDockSplitter(),
                        _rightDock
                    )
                },
                new ProportionalDockSplitter(),
                _bottomDock
            )
        };

        var rootDock = CreateRootDock();
        _rootDock = rootDock;
        rootDock.IsCollapsable = false;
        rootDock.ActiveDockable = mainLayout;
        rootDock.DefaultDockable = mainLayout;
        rootDock.VisibleDockables = CreateList<IDockable>(mainLayout);

        rootDock.LeftPinnedDockables = CreateList<IDockable>();
        rootDock.RightPinnedDockables = CreateList<IDockable>();
        rootDock.TopPinnedDockables = CreateList<IDockable>();
        rootDock.BottomPinnedDockables = CreateList<IDockable>();


        
        // Set factory references for proper reactive updates
        leftDock.Factory = this;
        rightDock.Factory = this;
        bottomDock.Factory = this;
        documentDock.Factory = this;

        // Don't integrate components here - let the UI finish loading first
        // Components will be integrated via MessageBus after full initialization
        return rootDock;
    }

    public void StoreComponents(IReadOnlyCollection<Services.ComponentRegistration> tools, IReadOnlyCollection<Services.ComponentRegistration> documents)
    {
        // Store components in singleton registry to avoid ephemeral state issues
        Services.ComponentRegistry.Instance.StoreComponents(tools, documents);
        
        Logger.Info($"Stored {tools.Count()} component tools and {documents.Count()} component documents in global registry");
        
        // Components will be integrated via MessageBus after UI is fully loaded
    }
    
    private readonly HashSet<Guid> _integratedComponentInstances = new();
    
    public void IntegrateComponentsAfterUILoad()
    {
        var registry = Services.ComponentRegistry.Instance;
        Logger.Info($"IntegrateComponentsAfterUILoad called - integrating {registry.ComponentDocuments.Count} documents and {registry.ComponentTools.Count} tools");
        
        // Integrate component documents using the same flow as opening files
        foreach (var componentDoc in registry.ComponentDocuments.Where(d => d.Position == DockComponent.Base.DockPosition.Document))
        {
            // Check if this component instance is already integrated
            if (_integratedComponentInstances.Contains(componentDoc.ComponentInstanceId))
            {
                Logger.Info($"Component document {componentDoc.Id} (Instance: {componentDoc.ComponentInstanceId}) already integrated - skipping");
                continue;
            }
            
            if (componentDoc.ViewModel is IDockable dockable)
            {
                Logger.Info($"Adding component document via dock integration: {componentDoc.Id} (Instance: {componentDoc.ComponentInstanceId})");
                
                // Use the same approach as opening a document - add to document dock
                if (_documentDock?.VisibleDockables != null)
                {
                    var currentDockables = _documentDock.VisibleDockables.ToList();
                    currentDockables.Add(dockable);
                    _documentDock.VisibleDockables = CreateList(currentDockables.ToArray());
                    
                    // Set as active to make it visible
                    _documentDock.ActiveDockable = dockable;
                    
                    // Mark this component instance as integrated
                    _integratedComponentInstances.Add(componentDoc.ComponentInstanceId);
                    
                    Logger.Info($"Successfully integrated component document: {componentDoc.Id}");
                }
            }
        }
        
        // Integrate component tools into their dock positions
        if (registry.ComponentTools.Any())
        {
            Logger.Info($"Integrating {registry.ComponentTools.Count} component tools into dock layout");
            
            foreach (var tool in registry.ComponentTools)
            {
                // Check if this component instance is already integrated
                if (_integratedComponentInstances.Contains(tool.ComponentInstanceId))
                {
                    Logger.Info($"Component tool {tool.Id} (Instance: {tool.ComponentInstanceId}) already integrated - skipping");
                    continue;
                }
                
                if (tool.ViewModel is IDockable dockable)
                {
                    Logger.Info($"Integrating tool: {tool.Id} at position {tool.Position} (Instance: {tool.ComponentInstanceId})");
                    
                    switch (tool.Position)
                    {
                        case DockComponent.Base.DockPosition.Left:
                            _leftDock?.VisibleDockables ??= new List<IDockable>();
                            if (_leftDock?.VisibleDockables != null)
                            {
                                var leftDockables = _leftDock.VisibleDockables.ToList();
                                leftDockables.Add(dockable);
                                _leftDock.VisibleDockables = CreateList(leftDockables.ToArray());
                                _leftDock.ActiveDockable = dockable;
                                
                                // Force UI refresh by setting focus to the active dockable
                                if (_leftDock.ActiveDockable is IDockable activeDockable)
                                {
                                    this.SetFocusedDockable(_leftDock, activeDockable);
                                }
                                _leftDock.IsEmpty = _leftDock.VisibleDockables.Count == 0;
                                
                                // Mark this component instance as integrated
                                _integratedComponentInstances.Add(tool.ComponentInstanceId);

                                Logger.Info($"Successfully integrated {tool.Id} into left dock with UI refresh");
                            }
                            break;
                            
                        case DockComponent.Base.DockPosition.Right:
                            if (_rightDock?.VisibleDockables != null)
                            {
                                var rightDockables = _rightDock.VisibleDockables.ToList();
                                rightDockables.Add(dockable);
                                _rightDock.VisibleDockables = CreateList(rightDockables.ToArray());
                                _rightDock.ActiveDockable = dockable;
                                
                                // Force UI refresh by setting focus to the active dockable
                                if (_rightDock.ActiveDockable is IDockable activeDockable)
                                {
                                    this.SetFocusedDockable(_rightDock, activeDockable);
                                }
                                _rightDock.IsEmpty = _rightDock.VisibleDockables.Count == 0;
                                
                                // Mark this component instance as integrated
                                _integratedComponentInstances.Add(tool.ComponentInstanceId);

                                Logger.Info($"Successfully integrated {tool.Id} into right dock with UI refresh");
                            }
                            break;
                            
                        case DockComponent.Base.DockPosition.Bottom:
                            if (_bottomDock?.VisibleDockables != null)
                            {
                                var bottomDockables = _bottomDock.VisibleDockables.ToList();
                                bottomDockables.Add(dockable);
                                _bottomDock.VisibleDockables = CreateList(bottomDockables.ToArray());
                                _bottomDock.ActiveDockable = dockable;
                                
                                // Force UI refresh by setting focus to the active dockable
                                if (_bottomDock.ActiveDockable is IDockable activeDockable)
                                {
                                    this.SetFocusedDockable(_bottomDock, activeDockable);
                                }

                                //_bottomDock.Title = "Bottom";
                                _bottomDock.IsEmpty = _bottomDock.VisibleDockables.Count == 0;
                                
                                // Mark this component instance as integrated
                                _integratedComponentInstances.Add(tool.ComponentInstanceId);
                                
                                Logger.Info($"Successfully integrated {tool.Id} into bottom dock with UI refresh");
                            }
                            break;
                            
                        default:
                            Logger.Warn($"Unknown dock position for tool {tool.Id}: {tool.Position}");
                            break;
                    }
                }
                else
                {
                    Logger.Warn($"Tool {tool.Id} ViewModel is not IDockable: {tool.ViewModel?.GetType().Name}");
                }
            }
        }
    }

    public override void InitLayout(IDockable layout)
    {
        
        DockableLocator = new Dictionary<string, Func<IDockable?>>()
        {
            ["Root"] = () => _rootDock,
            ["Documents"] = () => _documentDock
        };

        HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
        {
            [nameof(IDockWindow)] = () => new HostWindow()
        };

        base.InitLayout(layout);
    }

    public void OpenDocument(string filePath)
    {
        OpenDocument(filePath, null);
    }

    public void OpenDocument(string filePath, int? targetLine)
    {
        Logger.Info($"[DockFactory] Document opening requested: {filePath}" + 
                   (targetLine.HasValue ? $" at line {targetLine.Value}" : ""));
        
        // Relay document opening to Editor component via message bus
        BroadcastFileNavigationMessage(filePath, targetLine ?? 0);
    }

    // Document handling removed - will be handled by Editor component

    public void NavigateToSourceLine(string filePath, int line)
    {
        try
        {
            Logger.Info($"[DockFactory] Relaying navigation request to components: {filePath}:{line}");
            
            // 🚀 NEW APPROACH: DockFactory acts as message relay hub instead of direct handler
            
            // 1. Send message to SolutionExplorer to highlight file (if it exists there)
            BroadcastFileSelectionMessage(filePath);
            
            // 2. Send message to Editor component to open document with line navigation  
            BroadcastFileNavigationMessage(filePath, line);
            
            Logger.Info($"[DockFactory] Message relay completed for {System.IO.Path.GetFileName(filePath)}:{line} - components will handle");
            
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to relay navigation messages for {System.IO.Path.GetFileName(filePath)}:{line}: {ex.Message}");
        }
    }

    private void TryHighlightInSolutionExplorer(string filePath)
    {
        try
        {
            // Find the Solution Explorer tool in the dock layout
            var solutionExplorer = FindSolutionExplorerViewModel(_rootDock);
            if (solutionExplorer != null)
            {
                // Try to expand and highlight the file (if it exists in the project tree)
                solutionExplorer.TrySelectAndExpandToFile(filePath);
            }
        }
        catch (Exception ex)
        {
            // Don't let Solution Explorer issues break navigation
            System.Console.WriteLine($"[DockFactory] Could not highlight in Solution Explorer: {ex.Message}");
        }
    }

    private SolutionExplorerViewModel? FindSolutionExplorerViewModel(IDockable? dock)
    {
        if (dock == null) return null;
        
        if (dock is SolutionExplorerViewModel solutionExplorer)
            return solutionExplorer;
        
        if (dock is IDock dockContainer && dockContainer.VisibleDockables != null)
        {
            foreach (var child in dockContainer.VisibleDockables)
            {
                var result = FindSolutionExplorerViewModel(child);
                if (result != null) return result;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Broadcast file selection message for SolutionExplorer to highlight/expand to file
    /// </summary>
    private void BroadcastFileSelectionMessage(string filePath)
    {
        try
        {
            // Create message for SolutionExplorer to select/highlight file
            var fileSelectionMessage = new ComponentMessage(
                "DockFactory_FileSelected",
                JsonSerializer.Serialize(new { FilePath = filePath })
            );
            
            MessageBus.Current.SendMessage(fileSelectionMessage);
            Logger.Debug($"[DockFactory] Broadcasted file selection message: {System.IO.Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, $"[DockFactory] Failed to broadcast file selection message for {filePath}");
        }
    }
    
    /// <summary>
    /// Broadcast file navigation message for Editor component to open file and navigate to line
    /// </summary>
    private void BroadcastFileNavigationMessage(string filePath, int lineNumber)
    {
        try
        {
            // Create message for Editor to open file and navigate to line
            var navigationData = new
            {
                FilePath = filePath,
                LineNumber = lineNumber,
                Context = $"Navigation from DockFactory"
            };
            
            var navigationMessage = new ComponentMessage(
                "Editor_NavigateToSource",
                JsonSerializer.Serialize(navigationData)
            );
            
            MessageBus.Current.SendMessage(navigationMessage);
            Logger.Debug($"[DockFactory] Broadcasted file navigation message: {System.IO.Path.GetFileName(filePath)}:{lineNumber}");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"[DockFactory] Failed to broadcast file navigation message for {filePath}:{lineNumber}");
        }
    }
}