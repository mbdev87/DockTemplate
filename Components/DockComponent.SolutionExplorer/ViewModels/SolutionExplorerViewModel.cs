using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DockComponent.Base;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using DockComponent.SolutionExplorer.Messages;
using DockComponent.SolutionExplorer.Transport.ErrorListComponent;
using Dock.Model.Controls;
using Dock.Model.Core;
using NLog;

namespace DockComponent.SolutionExplorer.ViewModels;

public class SolutionExplorerViewModel : ReactiveObject, ITool, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    // ITool implementation
    [Reactive] public string Id { get; set; } = "SolutionExplorer";
    [Reactive] public string Title { get; set; } = "Solution Explorer";
    [Reactive] public object? Context { get; set; }
    [Reactive] public IDockable? Owner { get; set; }
    [Reactive] public IDockable? OriginalOwner { get; set; }
    [Reactive] public IFactory? Factory { get; set; }
    [Reactive] public bool IsEmpty { get; set; }
    [Reactive] public bool IsCollapsable { get; set; } = true;
    [Reactive] public double Proportion { get; set; } = double.NaN;
    [Reactive] public DockMode Dock { get; set; } = DockMode.Center;
    [Reactive] public int Column { get; set; } = 0;
    [Reactive] public int Row { get; set; } = 0;
    [Reactive] public int ColumnSpan { get; set; } = 1;
    [Reactive] public int RowSpan { get; set; } = 1;
    [Reactive] public bool IsSharedSizeScope { get; set; }
    [Reactive] public double CollapsedProportion { get; set; } = double.NaN;
    [Reactive] public bool CanClose { get; set; } = true;
    [Reactive] public bool CanPin { get; set; } = true;
    [Reactive] public bool CanFloat { get; set; } = true;
    [Reactive] public bool CanDrag { get; set; } = true;
    [Reactive] public bool CanDrop { get; set; } = true;
    [Reactive] public double MinWidth { get; set; } = double.NaN;
    [Reactive] public double MaxWidth { get; set; } = double.NaN;
    [Reactive] public double MinHeight { get; set; } = double.NaN;
    [Reactive] public double MaxHeight { get; set; } = double.NaN;
    [Reactive] public bool IsModified { get; set; }
    [Reactive] public string? DockGroup { get; set; }

    // SolutionExplorerViewModel specific properties
    [Reactive] public ObservableCollection<FileSystemItemViewModel> Items { get; set; } = new();
    [Reactive] public string RootPath { get; set; } = string.Empty;
    [Reactive] public FileSystemItemViewModel? SelectedItem { get; set; }

    public ICommand ExpandAllCommand { get; }
    public ICommand CollapseAllCommand { get; }
    public ICommand RefreshCommand { get; }

    public SolutionExplorerViewModel()
    {
        // Subscribe to navigation requests
        MessageBus.Current.Listen<ComponentMessage>()
            .Where(msg => msg.Name == SolutionExplorerMessages.NAVIGATE_TO_FILE)
            .Subscribe(message => HandleNavigateToFile(message))
            .DisposeWith(_disposables);

        MessageBus.Current.Listen<ComponentMessage>()
            .Where(msg => msg.Name == SolutionExplorerMessages.REFRESH_EXPLORER)
            .Subscribe(_ => LoadDirectory())
            .DisposeWith(_disposables);

        // Listen for error navigation messages from ErrorList
        MessageBus.Current.Listen<ComponentMessage>()
            .Where(msg => msg.Name == "ErrorList_ScrollToLine")
            .Subscribe(message => HandleErrorNavigation(message))
            .DisposeWith(_disposables);
        
        ExpandAllCommand = ReactiveCommand.Create(ExpandAll);
        CollapseAllCommand = ReactiveCommand.Create(CollapseAll);
        RefreshCommand = ReactiveCommand.Create(LoadDirectory);
        
        LoadDirectory();
    }

    private void LoadDirectory()
    {
        try
        {
            RootPath = new DirectoryInfo(Directory.GetCurrentDirectory())?.Parent?.Parent?.Parent?.Parent?.FullName ?? Directory.GetCurrentDirectory();
            Items.Clear();
            
            var rootItem = new FileSystemItemViewModel(RootPath, OnFileOpened);
            Items.Add(rootItem);
            
            LogInfo($"🗂️ Loaded directory: {RootPath}");
        }
        catch (Exception ex)
        {
            LogError($"❌ Error loading directory: {ex.Message}");
        }
    }

    private void ExpandAll()
    {
        LogInfo($"📂 Expanded all items - {Items.Count} root items");
        foreach (var item in Items)
        {
            item.ExpandAll();
        }
    }

    private void CollapseAll()
    {
        LogInfo($"🗁 Collapsed all items - {Items.Count} root items");
        foreach (var item in Items)
        {
            item.CollapseAll();
        }
    }

    private void OnFileOpened(string filePath)
    {
        LogInfo($"📂 File opened: {filePath}", filePath);
        
        // Send navigation message to Editor component
        var navMessage = new NavigateToSourceMessage
        {
            FilePath = filePath,
            LineNumber = 1,
            Column = 0,
            Context = "Opened from Solution Explorer"
        };
        
        var componentMessage = NavigateToSourceMessageTransport.Create(navMessage);
        MessageBus.Current.SendMessage(componentMessage);
        
        // Also emit file selected message (legacy)
        SolutionExplorerMessageHelper.EmitFileSelected(filePath);
    }

    private void HandleNavigateToFile(ComponentMessage message)
    {
        try
        {
            var navData = System.Text.Json.JsonSerializer.Deserialize<dynamic>(message.Payload);
            var filePath = navData.GetProperty("FilePath").GetString();
            if (filePath != null)
            {
                TrySelectAndExpandToFile(filePath);
            }
        }
        catch (Exception ex)
        {
            LogError($"❌ Error handling navigate to file: {ex.Message}");
        }
    }

    private void HandleErrorNavigation(ComponentMessage message)
    {
        try
        {
            var errorNavMsg = System.Text.Json.JsonSerializer.Deserialize<ErrorNavigationMsg>(message.Payload);
            if (errorNavMsg != null && !string.IsNullOrEmpty(errorNavMsg.FilePath))
            {
                // Check if file exists before attempting navigation
                if (!File.Exists(errorNavMsg.FilePath))
                {
                    LogWarn($"🚫 File does not exist, ignoring navigation request: {errorNavMsg.FilePath}", errorNavMsg.FilePath, errorNavMsg.LineNumber);
                    return;
                }
                
                LogInfo($"🎯 Error navigation request received for {Path.GetFileName(errorNavMsg.FilePath)}:{errorNavMsg.LineNumber}", errorNavMsg.FilePath, errorNavMsg.LineNumber);
                TrySelectAndExpandToFile(errorNavMsg.FilePath);
            }
        }
        catch (Exception ex)
        {
            LogError($"❌ Error handling error navigation: {ex.Message}");
        }
    }
    
    // Helper methods to send messages to both NLog and inter-plugin Output panel
    private void LogInfo(string message, string? filePath = null, int? lineNumber = null)
    {
        Logger.Info(message);
        SendLogMessage("Info", message, filePath, lineNumber);
    }
    
    private void LogError(string message, string? filePath = null, int? lineNumber = null)
    {
        Logger.Error(message);
        SendLogMessage("Error", message, filePath, lineNumber);
    }
    
    private void LogWarn(string message, string? filePath = null, int? lineNumber = null)
    {
        Logger.Warn(message);
        SendLogMessage("Warn", message, filePath, lineNumber);
    }
    
    private void SendLogMessage(string level, string message, string? filePath, int? lineNumber)
    {
        var logMessage = new LogMessage
        {
            Level = level,
            Message = message,
            Source = "Solution Explorer",
            Timestamp = DateTime.Now,
            FilePath = filePath,
            LineNumber = lineNumber
        };
        
        var componentMessage = LogMessageTransport.Create(logMessage);
        MessageBus.Current.SendMessage(componentMessage);
    }

    public void Dispose()
    {
        _disposables?.Dispose();
    }

    public void TrySelectAndExpandToFile(string filePath)
    {
        try
        {
            // Check if the file is within our project tree
            if (!filePath.StartsWith(RootPath, StringComparison.OrdinalIgnoreCase))
            {
                System.Console.WriteLine($"[SolutionExplorer] File {filePath} is outside project tree, skipping selection");
                return;
            }

            // Find and expand to the file
            foreach (var rootItem in Items)
            {
                if (TryExpandToFileRecursive(rootItem, filePath))
                {
                    System.Console.WriteLine($"[SolutionExplorer] Successfully expanded to file: {filePath}");
                    return;
                }
            }
            
            System.Console.WriteLine($"[SolutionExplorer] Could not find file in tree: {filePath}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[SolutionExplorer] Error expanding to file: {ex.Message}");
        }
    }

    private bool TryExpandToFileRecursive(FileSystemItemViewModel item, string targetPath)
    {
        // If this is the target file, we found it!
        if (!item.IsDirectory && string.Equals(item.FullPath, targetPath, StringComparison.OrdinalIgnoreCase))
        {
            // Select the file and mark it for scrolling
            SelectedItem = item;
            item.IsSelected = true;
            item.ShouldScrollIntoView = true;
            
            System.Console.WriteLine($"[SolutionExplorer] File selected: {item.Name}");
            return true;
        }

        // If this is a directory and the target path starts with our path, expand and recurse
        if (item.IsDirectory && targetPath.StartsWith(item.FullPath, StringComparison.OrdinalIgnoreCase))
        {
            if (!item.IsExpanded)
            {
                item.IsExpanded = true;
            }

            // Search children
            foreach (var child in item.Children)
            {
                if (TryExpandToFileRecursive(child, targetPath))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // ITool interface methods
    public string? GetControlRecyclingId() => Id;
    public virtual bool OnClose() => true;
    public virtual void OnSelected() { }

    public void GetVisibleBounds(out double x, out double y, out double width, out double height)
    {
        x = y = width = height = double.NaN;
    }

    public void SetVisibleBounds(double x, double y, double width, double height) { }
    public virtual void OnVisibleBoundsChanged(double x, double y, double width, double height) { }

    public void GetPinnedBounds(out double x, out double y, out double width, out double height)
    {
        x = y = width = height = double.NaN;
    }

    public void SetPinnedBounds(double x, double y, double width, double height) { }
    public virtual void OnPinnedBoundsChanged(double x, double y, double width, double height) { }

    public void GetTabBounds(out double x, out double y, out double width, out double height)
    {
        x = y = width = height = double.NaN;
    }

    public void SetTabBounds(double x, double y, double width, double height) { }
    public virtual void OnTabBoundsChanged(double x, double y, double width, double height) { }

    public void GetPointerPosition(out double x, out double y)
    {
        x = y = double.NaN;
    }

    public void SetPointerPosition(double x, double y) { }
    public virtual void OnPointerPositionChanged(double x, double y) { }

    public void GetPointerScreenPosition(out double x, out double y)
    {
        x = y = double.NaN;
    }

    public void SetPointerScreenPosition(double x, double y) { }
    public virtual void OnPointerScreenPositionChanged(double x, double y) { }
}
