using DockComponent.Base;
using DockComponent.ErrorList.ViewModels;
using DockComponent.ErrorList.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DockComponent.ErrorList
{
    public class ErrorListComponent : IDockComponent
    {
        public string Name => "Error List Component";
        public string Version => "1.0.0";
        public Guid InstanceId { get; } = Guid.NewGuid();

        public void Register(IDockComponentContext context)
        {
            // Register services in DI container for potential host use
            context.Services.AddSingleton<ErrorService>();
            context.Services.AddTransient<ErrorListViewModel>();
            
            // Load component styles - CRITICAL for Avalonia View discovery!
            var stylesUri = new Uri("avares://DockComponent.ErrorList/Styles.axaml");
            context.RegisterResources(stylesUri);
            
            // Create ErrorList tool with component-managed dependencies
            // Components maintain their own instance management for now
            var errorService = new ErrorService();
            var errorListViewModel = new ErrorListViewModel(errorService);
            
            context.RegisterTool("ErrorList", errorListViewModel, DockPosition.Bottom);
        }
    }
}