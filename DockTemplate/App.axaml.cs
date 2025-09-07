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
            // AUTHOR MODE - Direct component registration (no plugin loading)
            var dockFactory = _serviceProvider.GetService<DockFactory>();
            if (dockFactory != null)
            {
                Console.WriteLine("[App] AUTHOR MODE - Registering components directly from DI container");
                
                // Register components now that DockFactory is available
                Program.RegisterAllComponents();
                
                Console.WriteLine("[App] ✅ Component registration complete - all components available for debugging");
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