using System;
using System.Diagnostics;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Dock.Model.Controls;
using Dock.Model.Core;
using DockTemplate.Services;
using NLog;

namespace DockTemplate.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IFactory? _factory;
    
    [Reactive] public IRootDock? Layout { get; set; }
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public ICommand NewLayout { get; }

    public MainWindowViewModel(DockFactory dockFactory)
    {
        _factory = dockFactory;

        DebugFactoryEvents(_factory);

        var layout = _factory?.CreateLayout();
        if (layout is not null)
        {
            Logger.Info("Init layout");
            _factory?.InitLayout(layout);
        }
        Layout = layout;

        // Layout is ready to use directly

        NewLayout = ReactiveCommand.Create(ResetLayout);
    }

    public void InitLayout()
    {
        if (Layout is null)
        {
            return;
        }

        _factory?.InitLayout(Layout);
    }

    public void CloseLayout()
    {
        if (Layout is IDock dock)
        {
            if (dock.Close.CanExecute(null))
            {
                dock.Close.Execute(null);
            }
        }
    }

    public void ResetLayout()
    {
        if (Layout is not null)
        {
            if (Layout.Close.CanExecute(null))
            {
                Layout.Close.Execute(null);
            }
        }

        var layout = _factory?.CreateLayout();
        if (layout is not null)
        {
            _factory?.InitLayout(layout);
            Layout = layout;
        }
    }

    private void DebugFactoryEvents(IFactory factory)
    {
        factory.ActiveDockableChanged += (_, args) =>
        {
            Debug.WriteLine($"[ActiveDockableChanged] Title='{args.Dockable?.Title}'");
        };

        factory.FocusedDockableChanged += (_, args) =>
        {
            Debug.WriteLine($"[FocusedDockableChanged] Title='{args.Dockable?.Title}'");
        };

        factory.DockableAdded += (_, args) =>
        {
            Debug.WriteLine($"[DockableAdded] Title='{args.Dockable?.Title}'");
        };

        factory.DockableRemoved += (_, args) =>
        {
            Debug.WriteLine($"[DockableRemoved] Title='{args.Dockable?.Title}'");
        };
    }
}