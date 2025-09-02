using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Avalonia.Threading;
using Dock.Model.Controls;
using Dock.Model.Core;
using DockTemplate.Services;
using NLog;

namespace DockTemplate.ViewModels.Tools;

public class ErrorListViewModel : ReactiveObject, ITool
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly ErrorService _errorService;
    private readonly Action<string, int>? _navigateToSource;

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
    [Reactive] public string FilterText { get; set; } = string.Empty;
    [Reactive] public string SelectedSeverity { get; set; } = "All";

    public string[] SeverityLevels { get; } = { "All", "Error", "Warning" };

    public ICommand ClearErrorsCommand { get; }
    public ICommand NavigateToErrorCommand { get; }
    public ICommand CopyErrorCommand { get; }

    // Summary properties
    public int ErrorCount => _errorService.ErrorCount;
    public int WarningCount => _errorService.WarningCount; 
    public int TotalCount => Errors.Count;

    public string Summary => $"🔴 {ErrorCount} Errors  🟡 {WarningCount} Warnings";

    public ErrorListViewModel(ErrorService errorService, Action<string, int>? navigateToSource = null)
    {
        _errorService = errorService;
        _navigateToSource = navigateToSource;

        ClearErrorsCommand = ReactiveCommand.Create(ClearErrors);
        NavigateToErrorCommand = ReactiveCommand.Create<ErrorEntry>(NavigateToError);
        CopyErrorCommand = ReactiveCommand.Create<ErrorEntry>(CopyError);

        // Subscribe to error service property changes to update summary
        _errorService.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_errorService.ErrorCount) ||
                e.PropertyName == nameof(_errorService.WarningCount) ||
                e.PropertyName == nameof(_errorService.InfoCount))
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
        
        // Subscribe to errors collection changes
        _errorService.Errors.CollectionChanged += (s, e) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                this.RaisePropertyChanged(nameof(TotalCount));
                this.RaisePropertyChanged(nameof(Summary));
            }, DispatcherPriority.Background);
        };

        Logger.Info("ErrorListViewModel initialized");
    }

    private void ClearErrors()
    {
        _errorService.Clear();
        Logger.Info("Error list cleared");
    }

    private void NavigateToError(ErrorEntry? error)
    {
        if (error == null || string.IsNullOrWhiteSpace(error.Code) || !error.Line.HasValue) return;

        try
        {
            Logger.Info($"[ErrorList] Navigating to {error.Source}:{error.Line}");
            _navigateToSource?.Invoke(error.Code, error.Line.Value);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"[ErrorList] Failed to navigate to {error.Source}:{error.Line}");
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
            Logger.Info($"[ErrorList] Copied error: {text}");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "[ErrorList] Failed to copy error to clipboard");
        }
    }

    public void OnErrorDoubleClicked(ErrorEntry error)
    {
        NavigateToError(error);
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