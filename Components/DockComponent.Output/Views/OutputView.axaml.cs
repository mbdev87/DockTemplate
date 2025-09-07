using System;
using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using ReactiveUI;
using DockComponent.Output.ViewModels;
using NLog;

namespace DockComponent.Output.Views;

public partial class OutputView : UserControl
{
    private ListBox? _logListBox;
    private ScrollViewer? _scrollViewer;
    private bool _isAutoScrollEnabled = true;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public OutputView()
    {
        InitializeComponent();
        
        _logListBox = this.FindControl<ListBox>("LogListBox");
        
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Find the ScrollViewer inside the ListBox
        if (_logListBox != null)
        {
            _scrollViewer = _logListBox.FindLogicalDescendantOfType<ScrollViewer>();
        }
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is OutputViewModel viewModel)
        {
            // Subscribe to log entries changes for auto-scroll
            viewModel.LogEntries.CollectionChanged += OnLogEntriesChanged;
            
            // Subscribe to AutoScroll property changes
            viewModel.WhenAnyValue(x => x.AutoScroll)
                .Subscribe(autoScroll => _isAutoScrollEnabled = autoScroll);
        }
    }

    private void OnLogEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isAutoScrollEnabled && (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Reset))
        {
            // Auto-scroll to bottom when new entries are added or collection is refreshed
            Dispatcher.UIThread.Post(ScrollToEnd, DispatcherPriority.Background);
        }
    }

    private void ScrollToEnd()
    {
        try
        {
            if (_logListBox?.ItemCount > 0)
            {
                // Try multiple approaches to ensure scrolling works
                if (_scrollViewer != null)
                {
                    _scrollViewer.ScrollToEnd();
                }
                else
                {
                    // Fallback: scroll the ListBox directly
                    _logListBox.ScrollIntoView(_logListBox.ItemCount - 1);
                }
            }
        }
        catch (Exception ex)
        {
            // Log scroll errors for debugging
            Logger.Debug($"[OutputView] Error scrolling to end: {ex.Message}");
        }
    }
}