using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DockTemplate.ViewModels.Documents;
using DockTemplate.ViewModels.Tools;
using DockTemplate.Models.Documents;
using DockTemplate.Models.Tools;
using DockTemplate.Models;
using DockTemplate.Services;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using NLog;

namespace DockTemplate.ViewModels;

public class DockFactory : Factory
{
    private IRootDock? _rootDock;
    private IDocumentDock? _documentDock;
    private readonly TextMateService _textMateService;
    private readonly LoggingDataService _loggingDataService;
    private readonly ErrorService _errorService;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public DockFactory(TextMateService textMateService, LoggingDataService loggingDataService, ErrorService errorService)
    {
        _textMateService = textMateService;
        _loggingDataService = loggingDataService;
        _errorService = errorService;
    }

    public override IRootDock CreateLayout()
    {
        var readmeDocument = new DocumentViewModel("Readme", "Readme.txt", _textMateService);
        var dashboardDocument = new DashboardViewModel();

        // Set informative README content
        readmeDocument.SetContent(@"🔥 DockTemplate - Batteries Included Avalonia Starter
================================================================

Welcome to DockTemplate! This is your jumpstart into building beautiful 
Avalonia applications with a professional IDE-like interface.

🎯 WHAT IS THIS PROJECT?
========================
DockTemplate is a ""batteries included"" Avalonia application template that gives you:
• A complete dockable interface system (like Visual Studio)
• Built-in Solution Explorer with gorgeous Material Design icons
• Real-time logging output with filtering and search
• Text editor with syntax highlighting
• Error list with source navigation
• Professional light/dark theme switching
• All the boring setup done for you!

✨ KEY FEATURES
===============
🗂️  Solution Explorer - Navigate your files with beautiful icons
📝  Text Editor - Syntax highlighting for 50+ file types
📊  Output Tool - Real-time logs with filtering and auto-scroll
🐛  Error List - Click errors to jump to source code
🎨  Material Design - Crisp icons with VS Code-inspired colors
🌙  Theme System - Light/Dark mode switching
⚡  ReactiveUI - Modern MVVM with reactive patterns

🔗 BUILT WITH THESE AMAZING LIBRARIES
=====================================
• Avalonia UI Framework: https://avaloniaui.net/
• Dock Layout System: https://github.com/wieslawsoltes/Dock
• Material Design Icons: https://pictogrammers.com/library/mdi/
• FontAwesome Icons: https://fontawesome.com/
• ReactiveUI MVVM: https://reactiveui.net/
• AvaloniaEdit: https://github.com/AvaloniaUI/AvaloniaEdit
• TextMate Grammars: https://github.com/microsoft/vscode-textmate

🚀 GETTING STARTED
==================
1. Open files using the Solution Explorer (left panel)
2. Check the Output tool (bottom) for real-time application logs
3. Switch themes using the theme selector
4. Start building your amazing Avalonia app!

💡 PRO TIPS
===========
• The Solution Explorer shows different colored icons for each file type
• Use Ctrl+Shift+O to see all available commands
• The Output tool filters by log level - try ""Error"" or ""Debug""
• All tools are dockable - drag them around to customize your layout

🎉 HAVE FUN BUILDING!
====================
This template gives you everything you need to focus on building features
instead of setting up infrastructure. Delete what you don't need, 
add what you do need, and create something awesome!

Questions? Check out the libraries above or dive into the source code.
Everything is well-documented and ready to customize.

Happy coding! 🚀");
        
        var solutionExplorer = new SolutionExplorerViewModel(_textMateService, OpenDocument);
        var properties = new ToolViewModel("Properties", "Properties");
        var toolbox = new ToolViewModel("Toolbox", "Toolbox");
        var output = new OutputViewModel(_loggingDataService);
        var errorList = new ErrorListViewModel(_errorService, NavigateToSourceLine);
        var editor = new ToolViewModel("Editor", "Editor");

        // Set Context content for tools
        properties.Context = "📝 Properties - Inspect and edit object details";
        toolbox.Context = "🔧 Toolbox - Drag and drop Avalonia controls";
        output.Context = "📊 Output - Real-time application logs with filtering";
        errorList.Context = "🐛 Error List - Click errors to navigate to source";
        editor.Context = "⚡ Editor - Advanced text editing with syntax highlighting";

        var leftDock = new ProportionalDock
        {
            Proportion = 0.2,
            Orientation = Orientation.Vertical,
            ActiveDockable = null,
            VisibleDockables = CreateList<IDockable>
            (
                new ToolDock
                {
                    ActiveDockable = solutionExplorer,
                    VisibleDockables = CreateList<IDockable>(solutionExplorer, editor),
                    Alignment = Alignment.Left,
                }
            ),
        };

        var rightDock = new ProportionalDock
        {
            Proportion = 0.2,
            Orientation = Orientation.Vertical,
            ActiveDockable = null,
            VisibleDockables = CreateList<IDockable>
            (
                new ToolDock
                {
                    ActiveDockable = toolbox,
                    VisibleDockables = CreateList<IDockable>(toolbox),
                    Alignment = Alignment.Top,
                },
                new ProportionalDockSplitter(),
                new ToolDock
                {
                    ActiveDockable = properties,
                    VisibleDockables = CreateList<IDockable>(properties),
                    Alignment = Alignment.Right,
                }
            ),
        };

        var bottomDock = new ToolDock
        {
            Proportion = 0.2,
            ActiveDockable = output,
            VisibleDockables = CreateList<IDockable>(output, errorList),
            Alignment = Alignment.Bottom,
        };

        var documentDock = new DocumentDock
        {
            IsCollapsable = false,
            ActiveDockable = readmeDocument,
            VisibleDockables = CreateList<IDockable>(readmeDocument, dashboardDocument),
            CanCreateDocument = true,
        };

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
                        leftDock,
                        new ProportionalDockSplitter(),
                        documentDock,
                        new ProportionalDockSplitter(),
                        rightDock
                    )
                },
                new ProportionalDockSplitter(),
                bottomDock
            )
        };

        var rootDock = CreateRootDock();

        rootDock.IsCollapsable = false;
        rootDock.ActiveDockable = mainLayout;
        rootDock.DefaultDockable = mainLayout;
        rootDock.VisibleDockables = CreateList<IDockable>(mainLayout);

        rootDock.LeftPinnedDockables = CreateList<IDockable>();
        rootDock.RightPinnedDockables = CreateList<IDockable>();
        rootDock.TopPinnedDockables = CreateList<IDockable>();
        rootDock.BottomPinnedDockables = CreateList<IDockable>();

        _documentDock = documentDock;
        _rootDock = rootDock;

        return rootDock;
    }

    public override void InitLayout(IDockable layout)
    {
        ContextLocator = new Dictionary<string, Func<object?>>
        {
            ["SolutionExplorer"] = () => new SolutionExplorerModel(),
            ["Dashboard"] = () => new PropertiesModel { Name = "Dashboard", Content = "📊 Project Analytics Dashboard\n==========================================\n\nAnalyzing project files...\n\n📁 File System Scan:\n• Counting files by type\n• Measuring file sizes\n• Analyzing code lines\n\n📈 Interactive Charts:\n• File type distribution (pie chart)\n• File sizes comparison (bar chart) \n• Line count trends (line chart)\n\n📋 Sortable Data Grid:\n• All files with details\n• Click headers to sort\n• Multi-column sorting\n\n🔄 Click Refresh to update data" },
            ["Properties"] = () => new PropertiesModel(),
            ["Toolbox"] = () => new PropertiesModel { Name = "Toolbox", Content = "🔧 Avalonia Controls:\n\n📋 Layout:\n• Panel\n• Grid\n• StackPanel\n• WrapPanel\n• DockPanel\n\n🎛️ Input:\n• Button\n• TextBox\n• ComboBox\n• CheckBox\n• RadioButton\n\n📝 Display:\n• TextBlock\n• Label\n• Image\n• TreeView\n• ListBox" },
            ["Output"] = () => new PropertiesModel { Name = "Output", Content = "📊 Output Window\n==========================================\n\nWelcome to DockTemplate!\n\n✅ Application initialized successfully\n📁 Solution Explorer loaded\n🎨 Material Design icons active\n🌙 Theme system ready\n\n🔍 Use the dropdown above to filter by log level\n🔎 Use the search box to find specific messages\n\nStart building your Avalonia app! 🚀" },
            ["ErrorList"] = () => new PropertiesModel { Name = "Error List", Content = "🐛 Error List\n==========================================\n\n✅ No errors found!\n\n⚠️  0 Errors\n📝 0 Warnings  \nℹ️  0 Messages\n\n🎉 Your code is clean and ready to go!" },
            ["Editor"] = () => new EditorToolModel()
        };

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
            var fileName = Path.GetFileName(filePath);
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            Logger.Info($"[DockFactory] Opening document: {fileName} from {filePath}" + 
                       (targetLine.HasValue ? $" at line {targetLine.Value}" : ""));
            
            // Check if document is already open
            var existingDocument = FindExistingDocument(filePath);
            if (existingDocument != null)
            {
                Logger.Info($"[DockFactory] Found existing document: {fileName}");
                
                // Focus existing document (even if already active)
                Logger.Info($"[DockFactory] Setting ActiveDockable to existing document: {existingDocument.Title}");
                _documentDock.ActiveDockable = existingDocument;
                
                // Navigate to specific line if specified - force this even if document is already active
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
                else
                {
                    Logger.Info($"[DockFactory] No target line specified for existing document {existingDocument.Title}");
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
            var document = new DocumentViewModel(documentId, fileName, _textMateService);
            
            // Store the file path for future lookups
            document.FilePath = filePath;
            
            // Load file content
            if (File.Exists(filePath))
            {
                try
                {
                    var content = File.ReadAllText(filePath);
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

    private DocumentViewModel? FindExistingDocument(string filePath)
    {
        if (_documentDock?.VisibleDockables == null) return null;
        
        foreach (var dockable in _documentDock.VisibleDockables)
        {
            if (dockable is DocumentViewModel doc && 
                string.Equals(doc.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
            {
                return doc;
            }
        }
        
        return null;
    }

    public void NavigateToSourceLine(string filePath, int line)
    {
        try
        {
            Logger.Info($"[DockFactory] Navigating to {filePath}:{line}");
            
            // Chain of awesome events! 🚀
            
            // 1. Try to find and highlight file in Solution Explorer (if it exists there)
            TryHighlightInSolutionExplorer(filePath);
            
            // 2. Open the document in editor with line highlighting and scrolling
            OpenDocument(filePath, line);
            
            Logger.Info($"[DockFactory] Navigation completed for {System.IO.Path.GetFileName(filePath)}:{line} with line highlighting");
            
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to navigate to {System.IO.Path.GetFileName(filePath)}:{line}");
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
}