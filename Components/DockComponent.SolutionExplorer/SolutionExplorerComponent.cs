using DockComponent.Base;
using DockComponent.SolutionExplorer.ViewModels;

namespace DockComponent.SolutionExplorer
{
    public class SolutionExplorerComponent : IDockComponent
    {
        public string Name => "Solution Explorer Component";
        public string Version => "1.0.0";
        public Guid InstanceId { get; } = Guid.NewGuid();

        public void Register(IDockComponentContext context)
        {
            // Register the SolutionExplorer tool
            context.RegisterTool("SolutionExplorer", new SolutionExplorerViewModel(), DockPosition.Left, isPrimary: true);
        }
    }
}