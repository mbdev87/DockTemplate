using Avalonia.Controls;
using DockComponent.Editor.ViewModels;

namespace DockComponent.Editor.Views;

public partial class EditorToolView : UserControl
{
    public EditorToolView()
    {
        InitializeComponent();
        
        DataContextChanged += OnDataContextChanged;
    }
    
    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is EditorToolViewModel viewModel)
        {
            // Setup TextMate for syntax highlighting when ViewModel is available
            viewModel.SetupTextMateForEditor(TextEditor);
        }
    }
}