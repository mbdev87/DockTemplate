using System;
using System.Linq;
using System.Threading;
using Avalonia;
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