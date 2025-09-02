using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DockTemplate.Views.Models;

public partial class SolutionExplorerModelView : UserControl
{
    public SolutionExplorerModelView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}