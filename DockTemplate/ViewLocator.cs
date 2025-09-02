using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DockTemplate.ViewModels;
using DockTemplate.ViewModels.Documents;
using DockTemplate.ViewModels.Tools;
using DockTemplate.Models.Documents;
using DockTemplate.Models.Tools;
using DockTemplate.Models;
using DockTemplate.Views;
using DockTemplate.Views.Documents;
using DockTemplate.Views.Tools;
using DockTemplate.Views.Models;
using Dock.Model.Core;
using ReactiveUI;

namespace DockTemplate;

public class ViewLocator : IDataTemplate, IViewLocator
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        return param switch
        {
            DocumentViewModel => new DocumentView(),
            SolutionExplorerViewModel => new SolutionExplorerView(),
            DashboardViewModel => new DashboardView(),
            OutputViewModel => new OutputView(),
            ErrorListViewModel => new ErrorListView(),
            ToolViewModel => new ToolView(),
            DocumentModel => new DocumentModelView(),
            SolutionExplorerModel => new SolutionExplorerModelView(),
            PropertiesModel => new PropertiesModelView(),
            EditorToolModel => new EditorToolView(),
            _ => BuildByConvention(param)
        };
    }

    private Control? BuildByConvention(object param)
    {
        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase or IDockable;
    }

    public IViewFor? ResolveView<T>(T? viewModel, string? contract = null)
    {
        return Build(viewModel) as IViewFor;
    }
}