using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using DockTemplate.Services;
using NLog;
using Avalonia.Threading;
using Dock.Model.Controls;
using Dock.Model.Core;

namespace DockTemplate.ViewModels.Tools;

public class OutputViewModel : ReactiveObject, ITool
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly LoggingService _loggingService;
    
    // ITool implementation
    [Reactive] public string Id { get; set; } = "Output";
    [Reactive] public string Title { get; set; } = "Output";
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

    // OutputViewModel specific properties
    [Reactive] public ObservableCollection<LogEntry> LogEntries { get; set; } = new();
    [Reactive] public bool AutoScroll { get; set; } = true;
    [Reactive] public string FilterText { get; set; } = string.Empty;
    [Reactive] public string SelectedLogLevel { get; set; } = "All";
    
    public ICommand ClearLogsCommand { get; }
    public ICommand ToggleAutoScrollCommand { get; }
    
    public string[] LogLevels { get; } = { "All", "Debug", "Info", "Warn", "Error", "Fatal" };
    
    public OutputViewModel(LoggingService loggingService)
    {
        _loggingService = loggingService;
        
        ClearLogsCommand = ReactiveCommand.Create(ClearLogs);
        ToggleAutoScrollCommand = ReactiveCommand.Create(() => AutoScroll = !AutoScroll);
        
        // Subscribe to logging service changes
        _loggingService.LogEntries.CollectionChanged += OnLogEntriesChanged;
        
        // Initial load of existing entries
        RefreshFilteredEntries();
        
        // Watch for filter changes
        this.WhenAnyValue(x => x.FilterText, x => x.SelectedLogLevel)
            .Subscribe(_ => 
            {
                Console.WriteLine($"[OutputViewModel] Filter changed - Level: '{SelectedLogLevel}', Text: '{FilterText}'");
                RefreshFilteredEntries();
            });
            
        Logger.Info("OutputViewModel initialized");
    }

    private void OnLogEntriesChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        RefreshFilteredEntries();
    }

    private void RefreshFilteredEntries()
    {
        try
        {
            var allEntries = _loggingService.LogEntries.ToList();
            var filtered = allEntries.AsEnumerable();
            
            Console.WriteLine($"[OutputViewModel] Filtering {allEntries.Count} entries, Level='{SelectedLogLevel}', Filter='{FilterText}'");
            
            // Filter by log level
            if (SelectedLogLevel != "All" && !string.IsNullOrEmpty(SelectedLogLevel))
            {
                filtered = filtered.Where(entry => entry.Level.Equals(SelectedLogLevel, StringComparison.OrdinalIgnoreCase));
            }
            
            // Filter by text
            if (!string.IsNullOrWhiteSpace(FilterText))
            {
                var filterLower = FilterText.ToLowerInvariant();
                filtered = filtered.Where(entry => 
                    entry.Message.ToLowerInvariant().Contains(filterLower) ||
                    entry.Logger.ToLowerInvariant().Contains(filterLower) ||
                    (entry.Exception?.ToLowerInvariant().Contains(filterLower) ?? false));
            }
            
            var filteredList = filtered.TakeLast(500).ToList(); // Limit displayed entries for performance
            
            Console.WriteLine($"[OutputViewModel] After filtering: {filteredList.Count} entries");
            
            // Update collection efficiently by comparing current vs new
            if (LogEntries.Count != filteredList.Count || !LogEntries.SequenceEqual(filteredList))
            {
                // Ensure UI updates happen on the UI thread
                Dispatcher.UIThread.Post(() =>
                {
                    LogEntries.Clear();
                    foreach (var entry in filteredList)
                    {
                        LogEntries.Add(entry);
                    }
                }, DispatcherPriority.Background);
                
                Console.WriteLine($"[OutputViewModel] Scheduled UI collection update to {filteredList.Count} entries");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OutputViewModel] Error refreshing entries: {ex.Message}");
        }
    }

    private void ClearLogs()
    {
        _loggingService.LogEntries.Clear();
        Dispatcher.UIThread.Post(() => LogEntries.Clear(), DispatcherPriority.Background);
        Logger.Info("Output logs cleared");
    }
    
    public void ScrollToEnd()
    {
        // This will be called from the View when AutoScroll is enabled
        // and new entries are added
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