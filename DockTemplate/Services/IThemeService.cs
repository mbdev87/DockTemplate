using System.Threading.Tasks;

namespace DockTemplate.Services;

public interface IThemeService
{
    void Switch(int index);
    Task InitializeFromSettingsAsync();
    void InitializeFromSettingsSync();
}