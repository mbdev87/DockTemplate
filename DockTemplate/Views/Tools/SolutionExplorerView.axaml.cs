using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DockTemplate.ViewModels.Tools;
using NLog;

namespace DockTemplate.Views.Tools;

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