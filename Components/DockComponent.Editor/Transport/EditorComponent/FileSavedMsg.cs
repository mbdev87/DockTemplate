using System.Text.Json;
using ReactiveUI;
using DockComponent.Base;

namespace DockComponent.Editor.Transport.EditorComponent;

/// <summary>
/// Message emitted when Editor saves a file
/// Contract: EditorComponent_FileSaved (v1)
/// </summary>
public record FileSavedMsg(string FilePath, string FileName, DateTime Timestamp);

public static class FileSavedHelper
{
    public static void Emit(string filePath, string fileName)
    {
        var data = new FileSavedMsg(filePath, fileName, DateTime.UtcNow);
        var json = JsonSerializer.Serialize(data);
        
        var message = new ComponentMessage("EditorComponent_FileSaved", json);
        MessageBus.Current.SendMessage(message);
    }
}