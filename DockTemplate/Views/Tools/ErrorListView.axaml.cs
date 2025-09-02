using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using DockTemplate.ViewModels.Tools;
using DockTemplate.Models;

namespace DockTemplate.Views.Tools;

public partial class ErrorListView : UserControl
{
    public ErrorListView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        // Setup double-click handling for navigation to source
        if (this.Find<DataGrid>("ErrorDataGrid") is { } dataGrid)
        {
            dataGrid.DoubleTapped += OnErrorDoubleClicked;
        }
    }

    private void OnErrorDoubleClicked(object? sender, TappedEventArgs e)
    {
        if (DataContext is ErrorListViewModel viewModel && 
            viewModel.SelectedError is { } selectedError)
        {
            viewModel.OnErrorDoubleClicked(selectedError);
        }
    }
}