using System;

namespace DockComponent.Editor.Transport
{
    public class NavigationRequestMsg
    {
        public int LineNumber { get; set; }
        public string? SourceContext { get; set; } // E.g., "Error", "Warning", or error message
        public DateTime RequestTime { get; set; } = DateTime.Now;
        public Guid RequestId { get; set; } = Guid.NewGuid();

        public NavigationRequestMsg(int lineNumber, string? sourceContext = null)
        {
            LineNumber = lineNumber;
            SourceContext = sourceContext;
        }

        public override string ToString()
        {
            return $"NavigationRequest(Line:{LineNumber}, Context:{SourceContext}, ID:{RequestId.ToString()[..8]})";
        }

        public override bool Equals(object? obj)
        {
            if (obj is NavigationRequestMsg other)
            {
                return RequestId == other.RequestId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return RequestId.GetHashCode();
        }
    }
}