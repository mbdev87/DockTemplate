namespace DockTemplate.Messages;

public class ErrorNavigationMessage(
    string filePath,
    int lineNumber,
    string errorMessage,
    string errorLevel)
{
    public string FilePath { get; set; } = filePath;
    public int LineNumber { get; set; } = lineNumber;
    public string ErrorMessage { get; set; } = errorMessage;
    public string ErrorLevel { get; set; } = errorLevel;

    public override string ToString()
    {
        return
            $"ErrorNavigationMessage(File:{System.IO.Path.GetFileName(FilePath)}, Line:{LineNumber}, Level:{ErrorLevel})";
    }
}