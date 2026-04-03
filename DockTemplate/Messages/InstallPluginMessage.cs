namespace DockTemplate.Messages;

public class InstallPluginMessage(string pluginPath)
{
    public string PluginPath { get; } = pluginPath;

    public string PluginFileName { get; } =
        System.IO.Path.GetFileName(pluginPath);
}

public class PluginInstallationStartedMessage(string pluginFileName)
{
    public string PluginFileName { get; } = pluginFileName;
}

public class PluginInstallationCompletedMessage(
    string pluginFileName,
    bool success,
    string? errorMessage = null)
{
    public string PluginFileName { get; } = pluginFileName;
    public bool Success { get; } = success;
    public string? ErrorMessage { get; } = errorMessage;
}