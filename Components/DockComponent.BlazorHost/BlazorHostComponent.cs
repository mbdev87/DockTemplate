using DockComponent.Base;
using DockComponent.BlazorHost.ViewModels;
using DockComponent.BlazorHost.Services;
using FluentBlazorExample.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DockComponent.BlazorHost;

public class BlazorHostComponent : IDockComponent
{
    public string Name => "Web Host Component";
    public string Version => "1.0.0";
    public Guid InstanceId { get; } = Guid.NewGuid();

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
}