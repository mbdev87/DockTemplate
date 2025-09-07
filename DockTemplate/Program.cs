using System;
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

namespace DockTemplate;

sealed class Program
{
    public static IServiceProvider? ServiceProvider { get; private set; }

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
        
        // MINIMAL SHELL SERVICES ONLY - All business logic handled by components
        services.AddSingleton<App>();
        services.AddSingleton<IViewLocator, ViewLocator>();
        services.AddSingleton<IThemeService, ThemeService>(); // Basic theme switching
        services.AddSingleton<PluginInstallationService>(); // Plugin loading infrastructure
        services.AddSingleton<InterPluginLogger>(); // Inter-plugin messaging logger

        // Component loading system is now done in App.axaml.cs after Avalonia initialization
        // This avoids loading Avalonia resources before the UI framework is ready
        services.AddSingleton<ComponentLoader>(provider => 
            new ComponentLoader(provider.GetRequiredService<DockFactory>(), services));
        
        services.AddSingleton<DockFactory>();
        services.AddTransient<MainWindowViewModel>();
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