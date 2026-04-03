namespace DockComponent.SolutionExplorer.Transport.ErrorListComponent
{
    public class ErrorNavigationMsg(string filePath, int lineNumber)
    {
        public string FilePath { get; } = filePath;
        public int LineNumber { get; } = lineNumber;
    }
}