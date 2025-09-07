using JetBrains.Annotations;

namespace DockComponent.ErrorList.Messages.EditorComponent;

[UsedImplicitly]
public class ErrorNavigationMsg(string filePath, int lineNumber)
{
    
    [UsedImplicitly] public string FilePath { get; } = filePath;
    [UsedImplicitly] public int LineNumber { get; } = lineNumber;
}