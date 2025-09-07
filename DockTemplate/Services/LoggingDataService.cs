using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using DockComponent.Base;
using ReactiveUI;
using DockTemplate.Services;

namespace DockTemplate.Services;

public class LoggingDataService : ReactiveObject
{
    private readonly LoggingService _loggingService;
    private ObservableCollection<LogEntry> _filteredEntries = new();
    private string _filterText = string.Empty;
    private string _selectedLogLevel = "All";

    public LoggingDataService(LoggingService loggingService)
    {
        _loggingService = loggingService;
        
        // Subscribe to logging service changes
        _loggingService.LogEntries.CollectionChanged += OnLogEntriesChanged;
        
        // Initial load of existing entries
        RefreshFilteredEntries();
    }

    public ObservableCollection<LogEntry> FilteredEntries
    {
        get => _filteredEntries;
        private set => this.RaiseAndSetIfChanged(ref _filteredEntries, value);
    }

    public string FilterText
    {
        get => _filterText;
        set
        {
            this.RaiseAndSetIfChanged(ref _filterText, value);
            RefreshFilteredEntries();
        }
    }

    public string SelectedLogLevel
    {
        get => _selectedLogLevel;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedLogLevel, value);
            RefreshFilteredEntries();
        }
    }

    public string[] LogLevels { get; } = { "All", "Debug", "Info", "Warn", "Error", "Fatal" };

    public void ClearLogs()
    {
        _loggingService.LogEntries.Clear();
        RefreshFilteredEntries();
    }

    private void OnLogEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshFilteredEntries();
    }

    private void RefreshFilteredEntries()
    {
        try
        {
            var allEntries = _loggingService.LogEntries.ToList();
            var filtered = allEntries.AsEnumerable();
            
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
                    (entry.Exception?.ToLowerInvariant().Contains(filterLower) ?? false));
            }
            
            var filteredList = filtered.TakeLast(500).ToList(); // Limit displayed entries for performance
            
            // Update collection efficiently by comparing current vs new
            if (FilteredEntries.Count != filteredList.Count || !FilteredEntries.SequenceEqual(filteredList))
            {
                FilteredEntries.Clear();
                foreach (var entry in filteredList)
                {
                    FilteredEntries.Add(entry);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LoggingDataService] Error refreshing entries: {ex.Message}");
        }
    }
}