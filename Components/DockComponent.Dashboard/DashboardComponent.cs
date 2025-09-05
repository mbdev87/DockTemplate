using System;
using DockComponent.Base;
using DockComponent.Dashboard.ViewModels;
using DockComponent.Dashboard.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DockComponent.Dashboard;

public class DashboardComponent : IDockComponent
{
    public string Name => "Dashboard Component";
    public string Version => "1.0.0";

    public void Register(IDockComponentContext context)
    {
        // Register component services in DI container
        context.Services.AddSingleton<DashboardDataService>();
        
        // Load component styles
        var stylesUri = new Uri("avares://DockComponent.Dashboard/Styles.axaml");
        context.RegisterResources(stylesUri);

        // Register Dashboard as a document - DI resolution will happen later
        var dashboardViewModel = new DashboardViewModel();
        context.RegisterDocument("Dashboard", dashboardViewModel);
    }
}