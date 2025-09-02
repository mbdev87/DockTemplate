using Avalonia.Controls;
using Avalonia.Interactivity;
using DockTemplate.ViewModels.Documents;

namespace DockTemplate.Views.Documents;

public partial class DocumentView : UserControl
{
    public DocumentView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DocumentViewModel viewModel)
        {
            viewModel.SetupTextMateForEditor(TextEditor);
        }
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is DocumentViewModel viewModel)
        {
            viewModel.SetupTextMateForEditor(TextEditor);
        }
    }
}