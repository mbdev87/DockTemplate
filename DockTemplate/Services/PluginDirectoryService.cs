using System;
using System.IO;
using NLog;

namespace DockTemplate.Services;

public static class PluginDirectoryService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    /// <summary>
    /// Gets the LocalAppData plugin directory: %LOCALAPPDATA%\DockTemplate\Components\
    /// </summary>
    public static string GetLocalAppDataPluginPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "DockTemplate", "Components");
    }

    
    /// <summary>
    /// Gets all plugin directories to scan (LocalAppData only)
    /// </summary>
    public static string[] GetAllPluginPaths()
    {
        return new[]
        {
            GetLocalAppDataPluginPath(),    // Production plugins
        };
    }
    
    /// <summary>
    /// Ensures the LocalAppData plugin directory exists
    /// </summary>
    public static void EnsureLocalAppDataDirectoryExists()
    {
        try
        {
            var pluginPath = GetLocalAppDataPluginPath();
            if (!Directory.Exists(pluginPath))
            {
                Directory.CreateDirectory(pluginPath);
                Logger.Info($"Created LocalAppData plugin directory: {pluginPath}");
            }
            else
            {
                Logger.Info($"LocalAppData plugin directory exists: {pluginPath}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to create LocalAppData plugin directory");
        }
    }
}