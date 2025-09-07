using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using DockTemplate.ViewModels;
using DockTemplate.Views;
using DockTemplate.Services;
using DockComponent.ErrorList.ViewModels;
using DockComponent.Output.Models;
using DockComponent.ErrorList.Services;
using DockComponent.Editor.ViewModels.Documents;
using DockComponent.Editor.Services;
using DockComponent.SolutionExplorer.ViewModels;
using DockComponent.Output.ViewModels;
using DockComponent.Base;

namespace DockTemplate;

sealed class Program
{
    public static IServiceProvider? ServiceProvider { get; private set; }
    private static readonly List<Action> ComponentsToRegister = new();

    [STAThread]
    public static void Main(string[] args)
    {
        using var provider = Initialize();
        ServiceProvider = provider;
        
        BuildAvaloniaApp(provider).StartWithClassicDesktopLifetime(args);
    }

    private static ServiceProvider Initialize()
    {
        try
        {
            Console.WriteLine("[Initialize] Creating service collection...");
            var services = new ServiceCollection();
            
            Console.WriteLine("[Initialize] Configuring services...");
            ConfigureServices(services);
            
            Console.WriteLine("[Initialize] Building service provider...");
            var provider = services.BuildServiceProvider();
            
            Console.WriteLine("[Initialize] Service provider built successfully");
            return provider;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Initialize] ERROR: {ex.Message}");
            Console.WriteLine($"[Initialize] Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<LoggingService, LoggingService>();
        services.AddLogging();
        
        // SHELL SERVICES
        services.AddSingleton<App>();
        services.AddSingleton<IViewLocator, ViewLocator>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<InterPluginLogger>();
        services.AddSingleton<DockFactory>();
        services.AddTransient<MainWindowViewModel>();
        
        // COMPONENT SERVICES - Direct registration for author/debug mode
        RegisterEditorComponent(services);
        RegisterErrorListComponent(services);
        RegisterOutputComponent(services);
        RegisterSolutionExplorerComponent(services);
    }
    
    private static void RegisterEditorComponent(IServiceCollection services)
    {
        services.AddSingleton<TextMateService>();
        // DocumentViewModel is created dynamically by DockFactory
    }
    
    private static void RegisterErrorListComponent(IServiceCollection services)
    {
        services.AddSingleton<ErrorListViewModel>();
        
        // Component registration will happen after DockFactory is available
        // Store component for later registration
        ComponentsToRegister.Add(() => {
            var dockFactory = ServiceProvider?.GetRequiredService<DockFactory>();
            if (dockFactory != null)
            {
                var context = new DockComponentContext(dockFactory, services, Guid.NewGuid());
                var component = new DockComponent.ErrorList.ErrorListComponent();
                component.Register(context);
            }
        });
    }
    
    private static void RegisterOutputComponent(IServiceCollection services)
    {
        services.AddSingleton<OutputViewModel>();
        
        ComponentsToRegister.Add(() => {
            var dockFactory = ServiceProvider?.GetRequiredService<DockFactory>();
            if (dockFactory != null)
            {
                var context = new DockComponentContext(dockFactory, services, Guid.NewGuid());
                var component = new DockComponent.Output.OutputComponent();
                component.Register(context);
            }
        });
    }
    
    private static void RegisterSolutionExplorerComponent(IServiceCollection services)
    {
        services.AddSingleton<SolutionExplorerViewModel>();
        
        ComponentsToRegister.Add(() => {
            var dockFactory = ServiceProvider?.GetRequiredService<DockFactory>();
            if (dockFactory != null)
            {
                var context = new DockComponentContext(dockFactory, services, Guid.NewGuid());
                var component = new DockComponent.SolutionExplorer.SolutionExplorerComponent();
                component.Register(context);
            }
        });
    }
    
    public static void RegisterAllComponents()
    {
        Console.WriteLine($"[Program] Registering {ComponentsToRegister.Count} components...");
        foreach (var registerAction in ComponentsToRegister)
        {
            registerAction();
        }
        Console.WriteLine("[Program] Component registration complete");
    }


    public static AppBuilder BuildAvaloniaApp(IServiceProvider provider)
    {
        var builder = AppBuilder.Configure(provider.GetRequiredService<App>)
            .UsePlatformDetect()
            .WithInterFont()
            .UseReactiveUI()
            .LogToTrace();
            
        return builder;
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure(() => Initialize().GetRequiredService<App>())
            .UsePlatformDetect()
            .WithInterFont()
            .UseReactiveUI()
            .LogToTrace();
            
        return builder;
    }
}