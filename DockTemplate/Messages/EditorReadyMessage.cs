using System;

namespace DockTemplate.Messages;

public class EditorReadyMessage
{
    public string FilePath { get; set; }
    public string DocumentTitle { get; set; }
    public DateTime ReadyTime { get; set; } = DateTime.Now;

    public EditorReadyMessage(string filePath, string documentTitle)
    {
        FilePath = filePath;
        DocumentTitle = documentTitle;
    }

    public override string ToString()
    {
        return $"EditorReadyMessage(File:{System.IO.Path.GetFileName(FilePath)}, Title:{DocumentTitle})";
    }
}