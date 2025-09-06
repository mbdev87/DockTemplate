namespace DockTemplate.Messages;

public class InstallPluginMessage
{
    public string PluginPath { get; }
    public string PluginFileName { get; }

    public InstallPluginMessage(string pluginPath)
    {
        PluginPath = pluginPath;
        PluginFileName = System.IO.Path.GetFileName(pluginPath);
    }
}

public class PluginInstallationStartedMessage
{
    public string PluginFileName { get; }

    public PluginInstallationStartedMessage(string pluginFileName)
    {
        PluginFileName = pluginFileName;
    }
}

public class PluginInstallationCompletedMessage
{
    public string PluginFileName { get; }
    public bool Success { get; }
    public string? ErrorMessage { get; }

    public PluginInstallationCompletedMessage(string pluginFileName, bool success, string? errorMessage = null)
    {
        PluginFileName = pluginFileName;
        Success = success;
        ErrorMessage = errorMessage;
    }
}