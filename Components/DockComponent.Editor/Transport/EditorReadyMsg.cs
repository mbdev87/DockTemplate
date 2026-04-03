// ReSharper disable UnusedMember.Global

namespace DockComponent.Editor.Transport
{
    public class EditorReadyMsg(string filePath, string title)
    {
        public string FilePath { get; } = filePath;
        public string Title { get; } = title;
    }
}