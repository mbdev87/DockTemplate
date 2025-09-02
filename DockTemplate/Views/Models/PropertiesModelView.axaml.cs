using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DockTemplate.Views.Models;

public partial class PropertiesModelView : UserControl
{
    public PropertiesModelView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}