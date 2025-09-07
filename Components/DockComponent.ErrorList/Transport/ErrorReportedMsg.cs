using System;

namespace DockComponent.ErrorList.Transport
{
    public class ErrorReportedMsg
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
        public string? Source { get; set; }
        public int? Line { get; set; }
        public string? Code { get; set; }
        public string? LoggerName { get; set; }
    }
}