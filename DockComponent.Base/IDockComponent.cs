using Microsoft.Extensions.DependencyInjection;

namespace DockComponent.Base;

public enum DockPosition
{
    Left,
    Right,
    Bottom,
    Top,
    Document
}

public interface IDockComponent
{
    string Name { get; }
    string Version { get; }
    void Register(IDockComponentContext context);
}

public interface IDockComponentContext
{
    // UI Registration
    void RegisterResources(Uri resourceUri);
    void RegisterTool(string id, object toolViewModel, DockPosition position = DockPosition.Left);
    void RegisterDocument(string id, object documentViewModel, DockPosition position = DockPosition.Document);
    
    // Service Registration - DI Container access
    IServiceCollection Services { get; }
}