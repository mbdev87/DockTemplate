using System.Text.Json;
using ReactiveUI;
using DockComponent.Base;

namespace DockComponent.Editor.Transport.EditorComponent;

/// <summary>
/// Message emitted when Editor opens a file
/// Contract: EditorComponent_FileOpened (v1)
/// </summary>
public record FileOpenedMsg(string FilePath, string FileName, string Language);

public static class FileOpenedHelper
{
    public static void Emit(string filePath, string fileName, string language)
    {
        var data = new FileOpenedMsg(filePath, fileName, language);
        var json = JsonSerializer.Serialize(data);
        
        var message = new ComponentMessage("EditorComponent_FileOpened", json);
        MessageBus.Current.SendMessage(message);
    }
}