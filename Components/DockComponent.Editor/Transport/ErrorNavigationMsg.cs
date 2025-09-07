namespace DockComponent.Editor.Transport
{
    public class ErrorNavigationMsg
    {
        public string FilePath { get; }
        public int LineNumber { get; }

        public ErrorNavigationMsg(string filePath, int lineNumber)
        {
            FilePath = filePath;
            LineNumber = lineNumber;
        }
    }
}