using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Media;
using DockComponent.Base;
using DockComponent.ErrorList.ViewModels;
using JetBrains.Annotations;


namespace DockComponent.ErrorList.Views;

[UsedImplicitly]
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
            // Also handle selection changed to ensure we track the selected error
            dataGrid.SelectionChanged += OnSelectionChanged;
        }

        // Setup test errors button
        if (this.Find<Button>("TestErrorsButton") is { } testButton)
        {
            testButton.Click += OnTestErrorsClicked;
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is DataGrid dataGrid && DataContext is ErrorListViewModel viewModel)
        {
            viewModel.SelectedError = dataGrid.SelectedItem as ErrorEntry;
        }
    }

    private void OnErrorDoubleClicked(object? sender, TappedEventArgs e)
    {
        if (DataContext is ErrorListViewModel viewModel && 
            viewModel.SelectedError is { } selectedError)
        {
            System.Console.WriteLine($"[ErrorListView] Double-clicked on error: {selectedError.Message}");
            viewModel.OnErrorDoubleClicked(selectedError);
        }
        else
        {
            System.Console.WriteLine("[ErrorListView] Double-click but no selected error");
        }
    }

    private void OnTestErrorsClicked(object? sender, RoutedEventArgs e)
    {
        // Add test error to demonstrate functionality
        if (DataContext is ErrorListViewModel viewModel)
        {
            viewModel.GenerateTestErrors();
        }
    }
}

public class ErrorLevelToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "Error" => "❌",
            "Fatal" => "💀", 
            "Warn" => "⚠️",
            "Warning" => "⚠️",
            _ => "ℹ️"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ErrorLevelToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var brush = value?.ToString() switch
        {
            "Error" => Brushes.Red,
            "Fatal" => Brushes.DarkRed,
            "Warn" => Brushes.Orange,
            "Warning" => Brushes.Orange,
            _ => Brushes.Blue
        };
        return brush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}