using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using DockComponent.Output.Models;
using DockComponent.Output.Messages; // Using LOCAL messages only - no cross-component dependencies!
using DockComponent.Base;
using NLog;
using Avalonia.Threading;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;

namespace DockComponent.Output.ViewModels;

public class OutputViewModel : ReactiveObject, ITool
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly LoggingDataService _loggingDataService;
    
    // ITool implementation
    [Reactive] public string Id { get; set; } = "Output";
    [Reactive] public string Title { get; set; } = "Output";
    [Reactive] public object? Context { get; set; } = "Output view";
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

    // OutputViewModel specific properties
    public ObservableCollection<LogEntry> LogEntries => _loggingDataService.FilteredEntries;
    [Reactive] public bool AutoScroll { get; set; } = true;
    
    public string FilterText
    {
        get => _loggingDataService.FilterText;
        set => _loggingDataService.FilterText = value;
    }
    
    public string SelectedLogLevel
    {
        get => _loggingDataService.SelectedLogLevel;
        set => _loggingDataService.SelectedLogLevel = value;
    }
    
    public ICommand ClearLogsCommand { get; }
    public ICommand ToggleAutoScrollCommand { get; }
    public ICommand NavigateToSourceCommand { get; }
    
    public string[] LogLevels => _loggingDataService.LogLevels;
    
    public OutputViewModel(LoggingDataService loggingDataService)
    {
        _loggingDataService = loggingDataService;
        
        ClearLogsCommand = ReactiveCommand.Create(ClearLogs);
        ToggleAutoScrollCommand = ReactiveCommand.Create(() => AutoScroll = !AutoScroll);
        NavigateToSourceCommand = ReactiveCommand.Create<LogEntry>(NavigateToSource);
        
        Logger.Info("OutputViewModel initialized");
    }


    private void ClearLogs()
    {
        _loggingDataService.ClearLogs();
        Logger.Info("Output logs cleared");
    }
    
    public void ScrollToEnd()
    {
        // This will be called from the View when AutoScroll is enabled
        // and new entries are added
    }
    
    private void NavigateToSource(LogEntry logEntry)
    {
        if (logEntry?.IsNavigable == true)
        {
            var navMessage = new NavigateToSourceMessage
            {
                FilePath = logEntry.FilePath!,
                LineNumber = logEntry.LineNumber!.Value,
                Context = $"From log: {logEntry.Message}"
            };
            
            var componentMessage = NavigateToSourceMessageTransport.Create(navMessage);
            MessageBus.Current.SendMessage(componentMessage);
            
            Logger.Info($"🧭 Navigation requested: {logEntry.NavigationText}");
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
}