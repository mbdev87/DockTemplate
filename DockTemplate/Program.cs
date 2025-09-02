using System;
using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using DockTemplate.ViewModels;
using DockTemplate.Views;
using DockTemplate.Services;
using DockTemplate.ViewModels.Tools;

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
        var services = new ServiceCollection();
        ConfigureServices(services);
        return services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ErrorService>();
        services.AddSingleton<App>();
        services.AddSingleton<IViewLocator, ViewLocator>();
        services.AddSingleton<TextMateService>();
        services.AddSingleton<LoggingService>();
        services.AddSingleton<LoggingDataService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddTransient<ErrorListViewModel>();

        services.AddTransient<DockFactory>();
        services.AddTransient<MainWindowViewModel>();
    }

    public static AppBuilder BuildAvaloniaApp(IServiceProvider provider)
    {
        return AppBuilder.Configure(provider.GetRequiredService<App>)
            .UsePlatformDetect()
            .WithInterFont()
            .UseReactiveUI()
            .LogToTrace();
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure(() => Initialize().GetRequiredService<App>())
            .UsePlatformDetect()
            .WithInterFont()
            .UseReactiveUI()
            .LogToTrace();
    }
}