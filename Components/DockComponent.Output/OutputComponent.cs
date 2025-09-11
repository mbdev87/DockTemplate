using DockComponent.Base;
using DockComponent.Output.ViewModels;
using DockComponent.Output.Models;
using Microsoft.Extensions.DependencyInjection;

namespace DockComponent.Output
{
    public class OutputComponent : IDockComponent
    {
        public string Name => "Output Component";
        public string Version => "1.0.0";
        public Guid InstanceId { get; } = Guid.NewGuid();

        public void Register(IDockComponentContext context)
        {
            // Register services
            var loggingDataService = context.Services.FirstOrDefault(e => e.ServiceType == typeof(LoggingDataService))?.ImplementationInstance;
            if (loggingDataService == null)
            {
                var n = new LoggingDataService();
                context.Services.AddSingleton<LoggingDataService>(n);
                // Load component styles - CRITICAL for Avalonia View discovery!
                var stylesUri = new Uri("avares://DockComponent.Output/Styles.axaml");
                context.RegisterResources(stylesUri);
            
                // Register the Output tool - the factory will resolve dependencies
                context.RegisterTool("Output", new OutputViewModel(n), DockPosition.Bottom);
            }
            
           
        }
    }
}