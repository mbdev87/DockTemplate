using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using DockComponent.SolutionExplorer.ViewModels;
using NLog;

namespace DockComponent.SolutionExplorer.Views;

public partial class SolutionExplorerView : UserControl
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public SolutionExplorerView()
    {
        InitializeComponent();
        
        // Handle double-click for file opening
        var treeView = this.FindControl<TreeView>("FileTree");
        if (treeView != null)
        {
            treeView.DoubleTapped += OnTreeViewDoubleTapped;
        }

        // Watch for DataContext changes to subscribe to property notifications
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is SolutionExplorerViewModel viewModel)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SolutionExplorerViewModel.SelectedItem))
        {
            if (DataContext is SolutionExplorerViewModel viewModel && viewModel.SelectedItem != null)
            {
                // Use a slight delay to ensure the UI has updated
                Dispatcher.UIThread.Post(() =>
                {
                    ScrollToSelectedItem(viewModel.SelectedItem);
                }, DispatcherPriority.Background);
            }
        }
    }

    private void ScrollToSelectedItem(FileSystemItemViewModel selectedItem)
    {
        try
        {
            var treeView = this.FindControl<TreeView>("FileTree");
            if (treeView != null && selectedItem.ShouldScrollIntoView)
            {
                // First ensure the TreeView has focus so selection is visible
                treeView.Focus();
                
                // Try to bring the item into view
                treeView.ScrollIntoView(selectedItem);
                selectedItem.ShouldScrollIntoView = false; // Reset the flag
                
                System.Console.WriteLine($"[SolutionExplorerView] Scrolled to item: {selectedItem.Name}");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[SolutionExplorerView] Error scrolling to item: {ex.Message}");
        }
    }

    private void OnTreeViewDoubleTapped(object? sender, TappedEventArgs e)
    {
        var treeView = sender as TreeView;
        var selectedItem = treeView?.SelectedItem as FileSystemItemViewModel;
        
        if (selectedItem != null)
        {
            // Double-click opens files or toggles directory expansion
            selectedItem.OpenFileCommand.Execute(null);
        }
    }
}