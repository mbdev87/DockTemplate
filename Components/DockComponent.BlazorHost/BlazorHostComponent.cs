using DockComponent.Base;
using DockComponent.BlazorHost.ViewModels;
using DockComponent.BlazorHost.Services;
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
            
            // Register the Web Host tool - the factory will resolve dependencies
            // context.RegisterTool("BlazorHost", new BlazorHostViewModel(), DockPosition.Bottom);
            context.RegisterDocument("BlazorHost", new WebViewTestViewModel());

        }
    }
}