using System.Text.Json;
using ReactiveUI;
using DockComponent.Base;

namespace DockComponent.Output.Transport.OutputComponent;

/// <summary>
/// Message emitted when Output logs an entry
/// Contract: OutputComponent_LogEntry (v1)
/// </summary>
public record LogEntryMsg(string Level, string Message, string Source, DateTime Timestamp);

public static class LogEntryHelper
{
    public static void Emit(string level, string message, string source)
    {
        var data = new LogEntryMsg(level, message, source, DateTime.UtcNow);
        var json = JsonSerializer.Serialize(data);
        
        var message2 = new ComponentMessage("OutputComponent_LogEntry", json);
        MessageBus.Current.SendMessage(message2);
    }
}