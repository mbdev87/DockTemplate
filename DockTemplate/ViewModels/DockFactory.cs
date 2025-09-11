using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using DockComponent.Base;
using DockComponent.SolutionExplorer.ViewModels;
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
    private IToolDock? _leftDock;
    private IProportionalDock? _rightDock;
    private IToolDock? _bottomDock;
    
    // Public accessors for layout persistence
    public IDocumentDock? DocumentDock => _documentDock;
    public IToolDock? LeftDock => _leftDock;
    public IProportionalDock? RightDock => _rightDock;
    public IToolDock? BottomDock => _bottomDock;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    // Reference to acrylic layout manager for dynamic tool placement
    public Services.AcrylicLayoutManager? AcrylicLayoutManager { get; set; }
    
    // Note: Component registrations are now stored in the singleton ComponentRegistry
    // This factory creates MINIMAL layout - components will add themselves dynamically

    public DockFactory()
    {
        // Listen for navigation messages from components
        MessageBus.Current.Listen<ComponentMessage>()
            .Where(msg => msg.Name == "Editor_NavigateToSource")
            .Subscribe(HandleNavigationMessage);
            
        // Listen for UI loaded message to integrate components after full initialization
        MessageBus.Current.Listen<UILoadedMessage>()
            .Subscribe(_ =>
            {
                Logger.Info("[DockFactory] Received UILoadedMessage - integrating components");
                IntegrateComponentsAfterUILoad();
            });
    }
    
    private void HandleNavigationMessage(ComponentMessage message)
    {
        try
        {
            var navigationData = JsonSerializer.Deserialize<NavigationMessageData>(message.Payload);
            if (navigationData != null)
            {
                Logger.Info($"[DockFactory] Received navigation message for {navigationData.FilePath}:{navigationData.LineNumber}");
                NavigateToSourceLine(navigationData.FilePath, navigationData.LineNumber);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"[DockFactory] Failed to handle navigation message: {ex.Message}");
        }
    }
    
    // Simple class to deserialize navigation messages
    private class NavigationMessageData
    {
        public string FilePath { get; set; } = "";
        public int LineNumber { get; set; }
        public int Column { get; set; } = 0;
        public bool HighlightLine { get; set; } = true;
        public string? Context { get; set; }
    }

    public override IRootDock CreateLayout()
    {
        // Create completely minimal layout - NO components loaded yet
        // Components will register themselves dynamically through the plugin system
        
        Logger.Info("[DockFactory] Creating minimal empty layout - components will populate it");

        // Create empty docks that components can populate
        var leftDock = new ToolDock()
        {
            Proportion = 0.2,
            ActiveDockable = null, // Will be set when components are added
            VisibleDockables = CreateList<IDockable>(),
            Alignment = Alignment.Left,
            IsCollapsable = true,
            IsActive = true,
            CanCloseLastDockable = false
        };

        var rightDock = new ProportionalDock
        {
            Proportion = 0.2,
            Orientation = Orientation.Vertical,
            ActiveDockable = null, // Will be set when components are added
            VisibleDockables = CreateList<IDockable>(),
            IsCollapsable = true,
            IsActive = true,
            CanCloseLastDockable = false
        };

        var bottomDock = new ToolDock
        {
            Proportion = 0.2,
            ActiveDockable = null, // Will be set when components are added
            VisibleDockables = CreateList<IDockable>(),
            Alignment = Alignment.Bottom,
            IsCollapsable = true,
            IsActive = true,
            CanCloseLastDockable = false
        };

        var documentDock = new DocumentDock
        {
            IsCollapsable = false,
            ActiveDockable = null, // Will be set when documents are added
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
        
        // Clear integration tracking to allow re-integration of components into fresh layout
        _integratedComponentInstances.Clear();
        Logger.Info($"Cleared component integration tracking - all components will be re-integrated into the new layout");
        
        // Try to restore saved layout first (before component integration)
        TryRestoreSavedLayout();
        
        // Initialize acrylic layout manager with primary tool
        AcrylicLayoutManager?.InitializeAcrylicMode();
        
        RefreshLayoutAfterAcrylicToggle();
        
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
                            // Check if this tool should be in acrylic sidebar instead of normal left dock
                            if (AcrylicLayoutManager?.IsAcrylicLayoutActive == true && 
                                AcrylicLayoutManager?.IsToolInAcrylicSidebar(dockable) == true)
                            {
                                Logger.Info($"Skipping {tool.Id} from left dock - it's in acrylic sidebar");
                                // Mark as integrated but don't add to left dock
                                _integratedComponentInstances.Add(tool.ComponentInstanceId);
                            }
                            else
                            {
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
        
        // Auto-save layout after all components are integrated
        TrySaveCurrentLayout();
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
        if (_documentDock == null)
        {
            Logger.Info("[DockFactory] Document dock not initialized");
            return;
        }

        try
        {
            var fileName = System.IO.Path.GetFileName(filePath);
            var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            
            Logger.Info($"[DockFactory] Opening document: {fileName} from {filePath}" + 
                       (targetLine.HasValue ? $" at line {targetLine.Value}" : ""));
            
            // Check if document is already open
            var existingDocument = FindExistingDocument(filePath);
            if (existingDocument != null)
            {
                Logger.Info($"[DockFactory] Found existing document: {fileName}");
                
                // Focus existing document
                Logger.Info($"[DockFactory] Setting ActiveDockable to existing document: {existingDocument.Title}");
                _documentDock.ActiveDockable = existingDocument;
                
                // Navigate to specific line if specified
                if (targetLine.HasValue && targetLine.Value > 0)
                {
                    Logger.Info($"[DockFactory] About to navigate existing document {existingDocument.Title} to line {targetLine.Value}");
                    
                    // Use dispatcher to ensure proper timing
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        Logger.Info($"[DockFactory] Dispatcher executing - calling NavigateToLine on {existingDocument.Title}");
                        existingDocument.NavigateToLine(targetLine.Value, $"Error/Warning click to line {targetLine.Value}");
                        Logger.Info($"[DockFactory] NavigateToLine call completed for {existingDocument.Title}");
                    }, Avalonia.Threading.DispatcherPriority.Background);
                }
                
                Logger.Info($"[DockFactory] Focused existing document: {fileName}" + 
                           (targetLine.HasValue ? $" with line {targetLine.Value} highlighted" : ""));
                return;
            }
            
            // Check if this is an image file and log a warning
            var imageExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".svg" };
            if (imageExtensions.Contains(extension))
            {
                Logger.Error($"Cannot open binary file '{fileName}' in text editor");
                Logger.Warn($"File '{fileName}' is an image file. Consider using an image viewer instead.");
                return;
            }
            
            // Create new document view model with file path as ID for tracking
            var documentId = filePath; // Use full path as unique identifier
            var document = DockComponent.Editor.EditorComponent.CreateDocument(documentId, fileName);
            
            // Store the file path for future lookups
            document.FilePath = filePath;
            
            // Load file content
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    var content = System.IO.File.ReadAllText(filePath);
                    document.SetContent(content);
                    
                    // Navigate to specific line if specified
                    if (targetLine.HasValue && targetLine.Value > 0)
                    {
                        document.NavigateToLine(targetLine.Value, $"New document at line {targetLine.Value}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Failed to read file '{fileName}'");
                    document.SetContent($"Error reading file: {ex.Message}");
                }
            }
            
            // Add to document dock at the beginning (pushing others to the right)
            var visibleDockables = _documentDock.VisibleDockables?.ToList() ?? new List<IDockable>();
            visibleDockables.Insert(0, document);
            _documentDock.VisibleDockables = CreateList(visibleDockables.ToArray());
            
            // Set as active document
            _documentDock.ActiveDockable = document;
            
            Logger.Info($"[DockFactory] Document opened successfully: {fileName}" + 
                       (targetLine.HasValue ? $" with line {targetLine.Value} highlighted" : ""));
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"[DockFactory] Error opening document {filePath}: {ex.Message}");
        }
    }

    // Document handling removed - will be handled by Editor component

    public void NavigateToSourceLine(string filePath, int line)
    {
        try
        {
            Logger.Info($"[DockFactory] Navigating to source line: {filePath}:{line}");
            
            // 🚀 DIRECT APPROACH: DockFactory handles document opening directly like reference project
            
            // 1. Send message to SolutionExplorer to highlight file (if it exists there)
            BroadcastFileSelectionMessage(filePath);
            
            // 2. DockFactory directly opens/focuses the document with line navigation
            OpenDocument(filePath, line);
            
            Logger.Info($"[DockFactory] Navigation completed for {System.IO.Path.GetFileName(filePath)}:{line}");
            
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to navigate to {System.IO.Path.GetFileName(filePath)}:{line}: {ex.Message}");
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
    
    private DockComponent.Editor.ViewModels.Documents.DocumentViewModel? FindExistingDocument(string filePath)
    {
        if (_documentDock?.VisibleDockables == null) return null;
        
        foreach (var dockable in _documentDock.VisibleDockables)
        {
            if (dockable is DockComponent.Editor.ViewModels.Documents.DocumentViewModel doc && 
                string.Equals(doc.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
            {
                return doc;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Refresh layout after acrylic mode toggle - moves tools between normal dock and acrylic sidebar
    /// </summary>
    public void RefreshLayoutAfterAcrylicToggle()
    {
        if (AcrylicLayoutManager == null || _leftDock == null) return;
        
        var registry = Services.ComponentRegistry.Instance;
        var leftTools = registry.ComponentTools.Where(t => t.Position == DockComponent.Base.DockPosition.Left).ToList();
        
        if (AcrylicLayoutManager.IsAcrylicLayoutActive)
        {
            Logger.Info("🎨 Refreshing layout for ACRYLIC mode - moving primary tool to sidebar");
            
            // Clear left dock - tools will be re-added except the primary one
            if (_leftDock.VisibleDockables != null)
            {
                var normalLeftTools = leftTools
                    .Where(t => t.ViewModel is IDockable)
                    .Select(t => t.ViewModel as IDockable)
                    .Where(d => d != null && !AcrylicLayoutManager.IsToolInAcrylicSidebar(d))
                    .ToList();
                    
                _leftDock.VisibleDockables = CreateList(normalLeftTools.Cast<IDockable>().ToArray());
                _leftDock.ActiveDockable = normalLeftTools.FirstOrDefault();
                _leftDock.IsEmpty = normalLeftTools.Count == 0;
                
                Logger.Info($"Left dock now contains {normalLeftTools.Count} tools (primary moved to acrylic sidebar)");
            }
        }
        else
        {
            Logger.Info("🎨 Refreshing layout for NORMAL mode - moving all tools back to left dock");
            
            // Add ALL left tools back to normal dock
            if (_leftDock.VisibleDockables != null)
            {
                var allLeftTools = leftTools
                    .Where(t => t.ViewModel is IDockable)
                    .Select(t => t.ViewModel as IDockable)
                    .Where(d => d != null)
                    .ToList();
                    
                _leftDock.VisibleDockables = CreateList(allLeftTools.Cast<IDockable>().ToArray());
                _leftDock.ActiveDockable = allLeftTools.FirstOrDefault();
                _leftDock.IsEmpty = allLeftTools.Count == 0;
                
                Logger.Info($"Left dock now contains {allLeftTools.Count} tools (all tools restored)");
            }
        }
    }
    
    private void TryRestoreSavedLayout()
    {
        try
        {
            var layoutPersistence = Program.ServiceProvider?.GetService<IDockLayoutPersistence>();
            if (layoutPersistence != null)
            {
                Logger.Info("🔄 Attempting to restore saved dock layout...");
                _ = Task.Run(async () =>
                {
                    var restored = await layoutPersistence.LoadLayoutAsync();
                    if (restored)
                    {
                        Logger.Info("🔄 ✅ Dock layout restored from settings");
                    }
                    else
                    {
                        Logger.Info("🔄 No saved layout found or restoration failed - using default layout");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to restore saved layout");
        }
    }
    
    private void TrySaveCurrentLayout()
    {
        try
        {
            var layoutPersistence = Program.ServiceProvider?.GetService<IDockLayoutPersistence>();
            if (layoutPersistence != null)
            {
                Logger.Debug("🔄 Auto-saving current dock layout...");
                _ = Task.Run(async () => await layoutPersistence.SaveCurrentLayoutAsync());
            }
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Failed to auto-save layout");
        }
    }
}