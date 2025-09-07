namespace DockComponent.ErrorList.Transport
{
    public class ErrorClickedMsg
    {
        public string FilePath { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string ErrorLevel { get; set; } = string.Empty;
    }
}