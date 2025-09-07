namespace DockComponent.Editor.Transport
{
    public class EditorReadyMsg
    {
        public string FilePath { get; }
        public string Title { get; }

        public EditorReadyMsg(string filePath, string title)
        {
            FilePath = filePath;
            Title = title;
        }
    }
}