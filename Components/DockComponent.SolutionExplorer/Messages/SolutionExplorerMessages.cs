using System.Text.Json;
using DockComponent.Base;
using ReactiveUI;

namespace DockComponent.SolutionExplorer.Messages;

/// <summary>
/// SolutionExplorer Component Message Definitions
/// Convention: SolutionExplorerComponent_MessageName
/// </summary>
public static class SolutionExplorerMessages
{
    // Messages this component EMITS
    public const string FILE_SELECTED = "SolutionExplorerComponent_FileSelected";
    public const string DIRECTORY_EXPANDED = "SolutionExplorerComponent_DirectoryExpanded";
    public const string DIRECTORY_COLLAPSED = "SolutionExplorerComponent_DirectoryCollapsed";
    
    // Messages this component CONSUMES  
    public const string NAVIGATE_TO_FILE = "EditorComponent_NavigateToFile";
    public const string REFRESH_EXPLORER = "UIComponent_RefreshExplorer";
}

// Data transfer objects for SolutionExplorer messages
public record FileSelectedData(string FilePath, string FileName, string FileExtension, DateTime Timestamp);
public record DirectoryExpandedData(string DirectoryPath, int ItemCount, DateTime Timestamp);
public record DirectoryCollapsedData(string DirectoryPath, DateTime Timestamp);

// Helper class for SolutionExplorer message operations
public static class SolutionExplorerMessageHelper
{
    public static void EmitFileSelected(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        var data = new FileSelectedData(filePath, fileInfo.Name, fileInfo.Extension, DateTime.UtcNow);
        var json = JsonSerializer.Serialize(data);
        var message = new ComponentMessage(SolutionExplorerMessages.FILE_SELECTED, json);
        MessageBus.Current.SendMessage(message);
    }

    public static void EmitDirectoryExpanded(string directoryPath, int itemCount)
    {
        var data = new DirectoryExpandedData(directoryPath, itemCount, DateTime.UtcNow);
        var json = JsonSerializer.Serialize(data);
        var message = new ComponentMessage(SolutionExplorerMessages.DIRECTORY_EXPANDED, json);
        MessageBus.Current.SendMessage(message);
    }

    public static void EmitDirectoryCollapsed(string directoryPath)
    {
        var data = new DirectoryCollapsedData(directoryPath, DateTime.UtcNow);
        var json = JsonSerializer.Serialize(data);
        var message = new ComponentMessage(SolutionExplorerMessages.DIRECTORY_COLLAPSED, json);
        MessageBus.Current.SendMessage(message);
    }
}