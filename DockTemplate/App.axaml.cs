using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using DockTemplate.ViewModels;
using DockTemplate.Views;
using DockTemplate.Services;
using DockComponent.Base;

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
        Console.WriteLine($"[App] Constructor with IServiceProvider: {(_serviceProvider != null ? "SUCCESS" : "NULL")}");
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        if (_serviceProvider != null)
        {
            ThemeService = _serviceProvider.GetService<IThemeService>();
            
            // CRITICAL: Load theme BEFORE any UI is shown to prevent flicker
            if (ThemeService != null)
            {
                Console.WriteLine("[App] Loading theme from settings EARLY to prevent flicker...");
                // Use synchronous version during app initialization
                ThemeService.InitializeFromSettingsSync();
                Console.WriteLine("[App] ✅ Theme loaded early");
            }
            
            // CRITICAL: Load acrylic setting EARLY too
            var acrylicLayoutManager = _serviceProvider.GetService<AcrylicLayoutManager>();
            if (acrylicLayoutManager != null)
            {
                Console.WriteLine("[App] Loading acrylic setting EARLY...");
                acrylicLayoutManager.InitializeAcrylicModeEarly();
                Console.WriteLine("[App] ✅ Acrylic setting loaded early");
            }
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
        Console.WriteLine($"[App] OnFrameworkInitializationCompleted called with _serviceProvider: {(_serviceProvider != null ? "SUCCESS" : "NULL")}");
        if (_serviceProvider != null)
        {
            // Start hosted services manually (poor man's hosted services)
            Console.WriteLine("[App] DEBUG: Attempting to get PluginInstallationService from DI...");
            var pluginInstallationService = _serviceProvider.GetService<PluginInstallationService>();
            Console.WriteLine($"[App] DEBUG: PluginInstallationService resolved: {(pluginInstallationService != null ? "SUCCESS" : "NULL")}");
            if (pluginInstallationService != null)
            {
                // Start the plugin installation service
                _ = pluginInstallationService.StartAsync(CancellationToken.None);
                Console.WriteLine("[App] ✅ PluginInstallationService started");
            }
            else
            {
                Console.WriteLine("[App] ❌ PluginInstallationService is NULL - service not registered properly!");
            }
            
            // AUTHOR MODE - Direct component registration + plugin loading
            var dockFactory = _serviceProvider.GetService<DockFactory>();
            if (dockFactory != null)
            {
                Console.WriteLine("[App] AUTHOR MODE - Registering components directly from DI container");
                
                // Register built-in components now that DockFactory is available
                Program.RegisterAllComponents();
                
                Console.WriteLine("[App] ✅ Built-in component registration complete");
                
                // Also load any installed plugins from LocalAppData
                var componentLoader = _serviceProvider.GetService<ComponentLoader>();
                if (componentLoader != null)
                {
                    var localAppDataPath = Services.PluginDirectoryService.GetLocalAppDataPluginPath();
                    if (Directory.Exists(localAppDataPath))
                    {
                        Console.WriteLine($"[App] Loading installed plugins from: {localAppDataPath}");
                        componentLoader.LoadComponents(localAppDataPath);
                        Console.WriteLine("[App] ✅ Installed plugin loading complete");
                    }
                    else
                    {
                        Console.WriteLine("[App] No installed plugins directory found");
                    }
                }
                
                Console.WriteLine("[App] ✅ All components available for debugging");
                
                // Theme already loaded early in Initialize() - no need to load again
                
                // Initialize dock layout service
                var dockLayoutService = _serviceProvider.GetService<IDockLayoutService>();
                if (dockLayoutService != null)
                {
                    Console.WriteLine("[App] Initializing dock layout service...");
                    _ = dockLayoutService.RestoreLayoutAsync();
                }
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