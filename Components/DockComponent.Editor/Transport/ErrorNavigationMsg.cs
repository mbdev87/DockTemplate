namespace DockComponent.Editor.Transport
{
    public class ErrorNavigationMsg(string filePath, int lineNumber)
    {
        public string FilePath { get; } = filePath;
        public int LineNumber { get; } = lineNumber;
    }
}