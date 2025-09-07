using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using DockTemplate.ViewModels;
using DockTemplate.Views;
using DockTemplate.Services;

namespace DockTemplate;

public partial class App : Application
{
    public static IThemeService? ThemeService { get; private set; }
    private IServiceProvider? _serviceProvider;
    
    public IServiceProvider? GetServiceProvider() => _serviceProvider;
    
    public App()
    {
    }

    public App(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        if (_serviceProvider != null)
        {
            ThemeService = _serviceProvider.GetService<IThemeService>();
        }
        
        InitializePlatformSpecificMenu();
    }

    private void InitializePlatformSpecificMenu()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Console.WriteLine("[App] macOS detected - Application MenuBar ready for native export");
        }
        else
        {
            Console.WriteLine("[App] Windows/Linux detected - using Window-level in-app menu");
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (_serviceProvider != null)
        {
            // Start our poor man's hosted services
            var batchingService = _serviceProvider.GetService<LogBatchingService>();
            if (batchingService != null)
            {
                _ = batchingService.StartAsync(CancellationToken.None);
            }
            
            var pluginInstallationService = _serviceProvider.GetService<PluginInstallationService>();
            if (pluginInstallationService != null)
            {
                _ = pluginInstallationService.StartAsync(CancellationToken.None);
            }
            
            // Load components after Avalonia is initialized (avoids resource loading issues)
            var componentLoader = _serviceProvider.GetService<ComponentLoader>();
            var dockFactory = _serviceProvider.GetService<DockFactory>();
            if (componentLoader != null && dockFactory != null)
            {
                // Ensure LocalAppData directory exists
                Services.PluginDirectoryService.EnsureLocalAppDataDirectoryExists();
                
                // Load components from all plugin directories (LocalAppData + Development)
                var pluginPaths = Services.PluginDirectoryService.GetAllPluginPaths();
                
                Console.WriteLine($"[App] Loading installed plugins from {pluginPaths.Count()} directories:");
                foreach (var pluginPath in pluginPaths)
                {
                    Console.WriteLine($"  - {pluginPath}");
                    componentLoader.LoadComponents(pluginPath);
                }
                
                // Check what we loaded
                var registry = Services.ComponentRegistry.Instance;
                Console.WriteLine($"[App] Startup component loading complete:");
                Console.WriteLine($"  - Loaded components: {registry.LoadedComponents.Count}");
                Console.WriteLine($"  - Registered tools: {registry.ComponentTools.Count}");
                Console.WriteLine($"  - Registered documents: {registry.ComponentDocuments.Count}");
                
                // Components are now in ComponentRegistry via AddComponents() calls
                // Integration will happen via UILoadedMessage in DockFactory
                Console.WriteLine("[App] ✅ Plugin loading complete - components ready for integration");
            }
        }
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindowViewModel = _serviceProvider?.GetRequiredService<MainWindowViewModel>() 
                ?? throw new InvalidOperationException("Service provider not initialized");
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}