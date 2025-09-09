using System.Collections.Generic;
using System.Threading.Tasks;
using DockTemplate.Models;

namespace DockTemplate.Services;

public interface IDockLayoutService
{
    Task SaveCurrentLayoutAsync();
    Task RestoreLayoutAsync();
    
    void RegisterComponent(string componentId, string displayName, string dockPosition = "Left");
    void UnregisterComponent(string componentId);
    
    bool ShouldRestoreComponent(string componentId);
    ComponentPosition? GetComponentRestorePosition(string componentId);
    
    List<string> GetAvailableComponents();
    List<string> GetRemovedComponents();
}