using Dock.Model.Mvvm.Controls;

namespace DockTemplate.ViewModels.Tools;

public class ToolViewModel : Tool
{
    public ToolViewModel()
    {
    }

    public ToolViewModel(string id, string title)
    {
        Id = id;
        Title = title;
    }
}