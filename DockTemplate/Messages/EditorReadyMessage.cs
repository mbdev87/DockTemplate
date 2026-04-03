using System;

namespace DockTemplate.Messages;

public class EditorReadyMessage(string filePath, string documentTitle)
{
    public string FilePath { get; set; } = filePath;
    public string DocumentTitle { get; set; } = documentTitle;
    public DateTime ReadyTime { get; set; } = DateTime.Now;

    public override string ToString()
    {
        return
            $"EditorReadyMessage(File:{System.IO.Path.GetFileName(FilePath)}, Title:{DocumentTitle})";
    }
}