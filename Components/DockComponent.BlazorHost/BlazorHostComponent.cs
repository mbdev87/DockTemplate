using DockComponent.Base;
using DockComponent.BlazorHost.ViewModels;
using DockComponent.BlazorHost.Services;
using FluentBlazorExample.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DockComponent.BlazorHost;

public class BlazorHostComponent : IDockComponent, IDisposable
{
    public string Name => "Web Host Component";
    public string Version => "1.0.0";
    public Guid InstanceId { get; } = Guid.NewGuid();

    private BlazorServerHost? _blazorServer;
    private bool _disposed;

    public void Register(IDockComponentContext context)
    {
        // Register services
        var webHostManager = context.Services.FirstOrDefault(e => e.ServiceType == typeof(WebHostManager))?.ImplementationInstance;
        if (webHostManager == null)
        {
            var manager = new WebHostManager();
            context.Services.AddSingleton<WebHostManager>(manager);

            // Load component styles - CRITICAL for Avalonia View discovery!
            var stylesUri = new Uri("avares://DockComponent.BlazorHost/Styles.axaml");
            context.RegisterResources(stylesUri);

            // Start our own Blazor server
            StartBlazorServer(context);

            // Get shared services from main application for bi-directional communication
            var sharedThemeService = context.Services.FirstOrDefault(s => s.ServiceType == typeof(IThemeService))?.ImplementationInstance as IThemeService;
            var sharedDashboardService = context.Services.FirstOrDefault(s => s.ServiceType == typeof(IDashboardService))?.ImplementationInstance as IDashboardService;

            Console.WriteLine($"🔗 [BLAZOR HOST] Found shared services - Theme: {sharedThemeService != null}, Dashboard: {sharedDashboardService != null}");

            // Create direct FluentBlazor dashboard document
            var fluentDashboard = new FluentDashboardViewModel(sharedThemeService, sharedDashboardService);

            // Create BlazorHost for browser simulation (optional)
            var blazorHost = new BlazorHostViewModel();

            // Create embedded dashboard for web hosting (background service)
            var embeddedDashboard = new EmbeddedDashboardViewModel(sharedThemeService, sharedDashboardService);

            // Register documents/tools
            context.RegisterDocument("FluentDashboard", fluentDashboard);  // Primary dashboard
            context.RegisterDocument("BlazorHost", blazorHost);             // Browser simulator
            context.RegisterDocument("WebViewTest", new WebViewTestViewModel());
            context.RegisterDocument("EmbeddedDashboard", embeddedDashboard); // Web service

            // Wire up the embedded dashboard to automatically load in BlazorHost when ready
            embeddedDashboard.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(embeddedDashboard.CurrentUrl) && !string.IsNullOrEmpty(embeddedDashboard.CurrentUrl))
                {
                    var dashboardUrl = $"{embeddedDashboard.CurrentUrl}/dashboard-embedded";
                    Console.WriteLine($"🔗 [BLAZOR HOST] Auto-loading embedded dashboard: {dashboardUrl}");
                    blazorHost.LoadUrl(dashboardUrl);
                }
            };

            Console.WriteLine("✅ [BLAZOR HOST] Registered documents: FluentDashboard, BlazorHost, WebViewTest, EmbeddedDashboard");
        }
    }

    private void StartBlazorServer(IDockComponentContext context)
    {
        try
        {
            // Get logger from main app's services
            var loggerFactory = context.Services.FirstOrDefault(s => s.ServiceType == typeof(ILoggerFactory))?.ImplementationInstance as ILoggerFactory;
            var logger = loggerFactory?.CreateLogger<BlazorServerHost>() ?? new NullLogger<BlazorServerHost>();

            // Create and start our Blazor server
            _blazorServer = new BlazorServerHost(logger);

            // Start the server in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await _blazorServer.StartAsync(CancellationToken.None);
                    Console.WriteLine("🚀 [BLAZOR HOST] Plugin started its own Blazor server!");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to start Blazor server from plugin");
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [BLAZOR HOST] Failed to start Blazor server: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            if (_blazorServer != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _blazorServer.StopAsync(CancellationToken.None);
                        _blazorServer.Dispose();
                        Console.WriteLine("🛑 [BLAZOR HOST] Plugin stopped its Blazor server");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ [BLAZOR HOST] Error stopping Blazor server: {ex.Message}");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [BLAZOR HOST] Error disposing component: {ex.Message}");
        }
    }
}

// Null logger implementation for fallback
internal class NullLogger<T> : ILogger<T>
{
    public IDisposable BeginScope<TState>(TState state) => new NullDisposable();
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }

    private class NullDisposable : IDisposable
    {
        public void Dispose() { }
    }
}