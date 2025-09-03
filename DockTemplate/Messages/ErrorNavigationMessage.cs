namespace DockTemplate.Messages;

public class ErrorNavigationMessage
{
    public string FilePath { get; set; }
    public int LineNumber { get; set; }
    public string ErrorMessage { get; set; }
    public string ErrorLevel { get; set; }

    public ErrorNavigationMessage(string filePath, int lineNumber, string errorMessage, string errorLevel)
    {
        FilePath = filePath;
        LineNumber = lineNumber;
        ErrorMessage = errorMessage;
        ErrorLevel = errorLevel;
    }

    public override string ToString()
    {
        return $"ErrorNavigationMessage(File:{System.IO.Path.GetFileName(FilePath)}, Line:{LineNumber}, Level:{ErrorLevel})";
    }
}