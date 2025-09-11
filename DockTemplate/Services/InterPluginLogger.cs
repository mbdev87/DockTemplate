using System;
using DockComponent.Output.Messages;
using ReactiveUI;
using NLog;

namespace DockTemplate.Services;

// Service that hooks into NLog and sends log messages to Output component
public class InterPluginLogger : ILogger
{
    private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
    
    public void Debug(string message, string? filePath = null, int? lineNumber = null)
    {
        LogInternal("Debug", message, filePath, lineNumber);
    }
    
    public void Info(string message, string? filePath = null, int? lineNumber = null)
    {
        LogInternal("Info", message, filePath, lineNumber);
    }
    
    public void Warn(string message, string? filePath = null, int? lineNumber = null)
    {
        LogInternal("Warn", message, filePath, lineNumber);
    }
    
    public void Error(string message, string? filePath = null, int? lineNumber = null)
    {
        LogInternal("Error", message, filePath, lineNumber);
    }
    
    private void LogInternal(string level, string message, string? filePath, int? lineNumber)
    {
        // Log normally to NLog
        var nlogLevel = GetNLogLevel(level);
        Logger.Log(nlogLevel, message);
        
        // Also send to Output component via MessageBus
        var logMessage = new LogMessage
        {
            Level = level,
            Message = message,
            Source = "DockTemplate",
            Timestamp = DateTime.Now,
            FilePath = filePath,
            LineNumber = lineNumber
        };
        
        var componentMessage = LogMessageTransport.Create(logMessage);
        MessageBus.Current.SendMessage(componentMessage);
    }
    
    private static NLog.LogLevel GetNLogLevel(string level) => level switch
    {
        "Debug" => NLog.LogLevel.Debug,
        "Info" => NLog.LogLevel.Info,
        "Warn" => NLog.LogLevel.Warn,
        "Error" => NLog.LogLevel.Error,
        _ => NLog.LogLevel.Info
    };
    
    // Implement ILogger if you have specific interface requirements
    public void Log(string level, string message, string? filePath = null, int? lineNumber = null)
    {
        LogInternal(level, message, filePath, lineNumber);
    }
}

// Simple interface
public interface ILogger
{
    void Debug(string message, string? filePath = null, int? lineNumber = null);
    void Info(string message, string? filePath = null, int? lineNumber = null);
    void Warn(string message, string? filePath = null, int? lineNumber = null);
    void Error(string message, string? filePath = null, int? lineNumber = null);
    void Log(string level, string message, string? filePath = null, int? lineNumber = null);
}