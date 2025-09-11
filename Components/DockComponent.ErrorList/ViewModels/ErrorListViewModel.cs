using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Avalonia.Threading;
using Dock.Model.Controls;
using Dock.Model.Core;
using DockComponent.ErrorList.Services;
using DockComponent.ErrorList.Transport;
using DockComponent.ErrorList.Messages;
using DockComponent.Base;
using System.Text.Json;
using DockComponent.ErrorList.Messages.EditorComponent;
using NLog;

namespace DockComponent.ErrorList.ViewModels;

public class PendingHighlightRequest
{
    public string FilePath { get; set; }
    public int LineNumber { get; set; }
    public string ErrorMessage { get; set; }
    public string ErrorLevel { get; set; }
    public DateTime RequestTime { get; set; } = DateTime.Now;

    public PendingHighlightRequest(string filePath, int lineNumber, string errorMessage, string errorLevel)
    {
        FilePath = filePath;
        LineNumber = lineNumber;
        ErrorMessage = errorMessage;
        ErrorLevel = errorLevel;
    }

    public override string ToString()
    {
        return $"PendingHighlight(File:{System.IO.Path.GetFileName(FilePath)}, Line:{LineNumber})";
    }
}

public class ErrorListViewModel : ReactiveObject, ITool, IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly ErrorService _errorService;
    private readonly CompositeDisposable _disposables = new();
    
    private static readonly ConcurrentDictionary<string, PendingHighlightRequest> _pendingHighlights = new();

    // ITool implementation
    [Reactive] public string Id { get; set; } = "ErrorList";
    [Reactive] public string Title { get; set; } = "Error List";
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

    // ErrorListViewModel specific properties
    public ObservableCollection<ErrorEntry> Errors => _errorService.Errors;
    [Reactive] public ErrorEntry? SelectedError { get; set; }
    
    public string FilterText
    {
        get => _errorService.FilterText;
        set => _errorService.FilterText = value;
    }
    
    public string SelectedSeverity
    {
        get => _errorService.SelectedSeverity;
        set => _errorService.SelectedSeverity = value;
    }

    public string[] SeverityLevels { get; } = { "All", "Error", "Warning" };

    public ICommand ClearErrorsCommand { get; }
    public ICommand NavigateToErrorCommand { get; }
    public ICommand CopyErrorCommand { get; }

    // Summary properties
    public int ErrorCount => _errorService.ErrorCount;
    public int WarningCount => _errorService.WarningCount; 
    public int TotalCount => Errors.Count;

    public string Summary => $"🔴 {ErrorCount} Errors  🟡 {WarningCount} Warnings";

    public ErrorListViewModel(ErrorService errorService)
    {
        _errorService = errorService;

        ClearErrorsCommand = ReactiveCommand.Create(ClearErrors);
        NavigateToErrorCommand = ReactiveCommand.Create<ErrorEntry>(NavigateToError);
        CopyErrorCommand = ReactiveCommand.Create<ErrorEntry>(CopyError);

        // Subscribe to error service property changes to update summary
        _errorService.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_errorService.ErrorCount) ||
                e.PropertyName == nameof(_errorService.WarningCount))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    this.RaisePropertyChanged(nameof(ErrorCount));
                    this.RaisePropertyChanged(nameof(WarningCount));
                    this.RaisePropertyChanged(nameof(TotalCount));
                    this.RaisePropertyChanged(nameof(Summary));
                }, DispatcherPriority.Background);
            }
        };
        
        // Subscribe to editor ready messages for ping-pong pattern
        MessageBus.Current.Listen<ComponentMessage>()
            .Where(msg => msg.Name.EndsWith("_EditorReady"))
            .Subscribe(msg => OnEditorReady(msg))
            .DisposeWith(_disposables);
        
        // Subscribe to errors collection changes
        _errorService.Errors.CollectionChanged += (s, e) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                this.RaisePropertyChanged(nameof(TotalCount));
                this.RaisePropertyChanged(nameof(Summary));
            }, DispatcherPriority.Background);
        };


        LogInfo("✅ Error List component initialized");
    }

    private void ClearErrors()
    {
        _errorService.Clear();
        LogInfo("🗑️ Error list cleared");
    }

    private void NavigateToError(ErrorEntry? error)
    {
        if (error == null || string.IsNullOrWhiteSpace(error.Code) || !error.Line.HasValue) return;

        try
        {
            LogInfo($"🧭 Navigating to {error.Source}:{error.Line}", error.Code, error.Line);
            
            // Use pure message-based navigation - no more callbacks!
            OnErrorDoubleClicked(error);
        }
        catch (Exception ex)
        {
            LogError($"❌ Failed to navigate to {error.Source}:{error.Line}: {ex.Message}", error.Code, error.Line);
        }
    }

    private void CopyError(ErrorEntry? error)
    {
        if (error == null) return;

        try
        {
            var fileName = System.IO.Path.GetFileName(error.Source);
            var text = $"{error.Level}: {error.Message} ({fileName}:{error.Line})";
            // TODO: Copy to clipboard when Avalonia clipboard API is available
            LogInfo($"📋 Copied error: {text}", error.Code, error.Line);
        }
        catch (Exception ex)
        {
            LogError($"❌ Failed to copy error to clipboard: {ex.Message}");
        }
    }

    public void OnErrorDoubleClicked(ErrorEntry error)
    {
        var filePath = error.Code ?? "unknown";
        var lineNumber = error.Line ?? 1;
        
        var errorClickedMsg = new ErrorClickedMsg
        {
            FilePath = filePath,
            LineNumber = lineNumber,
            ErrorMessage = error.Message ?? "unknown error",
            ErrorLevel = error.Level ?? "Error"
        };
        
        var componentMessage = new ComponentMessage(
            "ErrorList_ErrorClicked",
            JsonSerializer.Serialize(errorClickedMsg)
        );
        
        MessageBus.Current.SendMessage(componentMessage);
        
        // Also send navigation message to Editor to open the file
        var navMessage = new NavigateToSourceMessage
        {
            FilePath = filePath,
            LineNumber = lineNumber,
            Context = $"Error: {error.Message}"
        };
        
        var navComponentMessage = NavigateToSourceMessageTransport.Create(navMessage);
        MessageBus.Current.SendMessage(navComponentMessage);
        
        // Also send message to SolutionExplorer to expand and focus on the file
        if (System.IO.File.Exists(filePath))
        {
            var expandFileMsg = new ComponentMessage(
                "ErrorList_ExpandFile",
                JsonSerializer.Serialize(new { FilePath = filePath })
            );
            MessageBus.Current.SendMessage(expandFileMsg);
        }
        
        var pendingRequest = new PendingHighlightRequest(
            filePath, 
            lineNumber, 
            error.Message ?? "unknown error",
            error.Level ?? "Error"
        );
        
        _pendingHighlights.TryAdd(filePath, pendingRequest);
        
        LogInfo($"🚀 Navigation messages sent for {System.IO.Path.GetFileName(filePath)}:{lineNumber}");
    }

    private void OnEditorReady(ComponentMessage message)
    {
        try
        {
            // Parse the editor ready message to get the file path
            var editorReadyData = JsonSerializer.Deserialize<JsonElement>(message.Payload);
            var filePath = editorReadyData.GetProperty("FilePath").GetString();
            
            if (filePath != null && _pendingHighlights.TryRemove(filePath, out var pendingRequest))
            {
                Logger.Info($"[ErrorList] Editor ready for {System.IO.Path.GetFileName(filePath)}, sending scroll message for line {pendingRequest.LineNumber}");
                
                // Send ErrorNavigationMsg to trigger line highlighting in the now-ready editor
                var scrollMsg = new ErrorNavigationMsg(
                    pendingRequest.FilePath,
                    pendingRequest.LineNumber
                );
                
                var componentMessage = new ComponentMessage(
                    "ErrorList_ScrollToLine", 
                    JsonSerializer.Serialize(scrollMsg)
                );
                
                Dispatcher.UIThread.Post(() =>
                {
                    MessageBus.Current.SendMessage(componentMessage);
                    Logger.Info($"[ErrorList] Scroll message sent for {System.IO.Path.GetFileName(filePath)}:{pendingRequest.LineNumber}");
                }, DispatcherPriority.Background);
            }
        }
        catch (Exception ex)
        {
            LogError($"❌ Failed to process editor ready message: {ex.Message}");
        }
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

    public void Dispose()
    {
        _disposables?.Dispose();
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
    
    public void GenerateTestErrors()
    {
        Logger.Error("Test error: double click on me to jump to source");
        Logger.Warn("Test warning: this is a sample warning message");
    }
    
    private void SendLogMessage(string level, string message, string? filePath, int? lineNumber)
    {
        var logMessage = new LogMessage
        {
            Level = level,
            Message = message,
            Source = "Error List",
            Timestamp = DateTime.Now,
            FilePath = filePath,
            LineNumber = lineNumber
        };
        
        var componentMessage = LogMessageTransport.Create(logMessage);
        MessageBus.Current.SendMessage(componentMessage);
    }
}