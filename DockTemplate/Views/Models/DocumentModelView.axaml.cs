using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DockTemplate.Views.Models;

public partial class DocumentModelView : UserControl
{
    public DocumentModelView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}