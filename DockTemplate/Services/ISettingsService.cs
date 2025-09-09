using System.Threading.Tasks;
using DockTemplate.Models;

namespace DockTemplate.Services;

public interface ISettingsService
{
    AppSettings Settings { get; }
    
    Task LoadSettingsAsync();
    Task SaveSettingsAsync();
    
    void SetTheme(bool isDarkTheme);
    void SetAcrylic(bool enableAcrylic);
    void SaveComponentPosition(string componentId, string dockPosition, bool isVisible, double? width = null, double? height = null, int order = 0);
    void MarkComponentRemoved(string componentId);
    ComponentPosition? GetComponentPosition(string componentId);
    bool IsComponentRemoved(string componentId);
}