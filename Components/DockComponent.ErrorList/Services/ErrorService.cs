using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using ReactiveUI;
using DockComponent.ErrorList.Messages;
using DockComponent.Base;
using System.Text.Json;
using NLog;

namespace DockComponent.ErrorList.Services
{
    public class ErrorService : ReactiveObject
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ObservableCollection<ErrorEntry> _errors = new();

        public ObservableCollection<ErrorEntry> Errors => _errors;

        public ErrorService()
        {
            // Subscribe to error messages from other components
            MessageBus.Current.Listen<ComponentMessage>()
                .Where(msg => msg.Name.EndsWith("_ErrorReported"))
                .Subscribe(msg =>
                {
                    try
                    {
                        var errorEntry = JsonSerializer.Deserialize<ErrorEntry>(msg.Payload);
                        if (errorEntry != null)
                        {
                            _errors.Insert(0, errorEntry);
                            UpdateCounts();
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"❌ Failed to deserialize error entry message: {ex.Message}");
                    }
                });

            // Log service initialization
            LogInfo("✅ Error Service initialized - listening for error messages from other components");
        }


        private void UpdateCounts()
        {
            this.RaisePropertyChanged(nameof(ErrorCount));
            this.RaisePropertyChanged(nameof(WarningCount));
        }

        public int ErrorCount => _errors.Count(e => e.Level == "Error");
        public int WarningCount => _errors.Count(e => e.Level == "Warning");

        public void Clear()
        {
            _errors.Clear();
            this.RaisePropertyChanged(nameof(ErrorCount));
            this.RaisePropertyChanged(nameof(WarningCount));
        }

        public void AddError(ErrorEntry error)
        {
            _errors.Insert(0, error);
            UpdateCounts();
        }
        
        // Helper methods to send messages to both NLog and inter-plugin Output panel
        private void LogInfo(string message, string? filePath = null, int? lineNumber = null)
        {
            Logger.Info(message);
            SendLogMessage("Info", message, filePath, lineNumber);
        }
        
        private void LogError(string message, string? filePath = null, int? lineNumber = null)
        {
            Logger.Error(message);
            SendLogMessage("Error", message, filePath, lineNumber);
        }
        
        private void LogWarn(string message, string? filePath = null, int? lineNumber = null)
        {
            Logger.Warn(message);
            SendLogMessage("Warn", message, filePath, lineNumber);
        }
        
        private void SendLogMessage(string level, string message, string? filePath, int? lineNumber)
        {
            var logMessage = new LogMessage
            {
                Level = level,
                Message = message,
                Source = "Error Service",
                Timestamp = DateTime.Now,
                FilePath = filePath,
                LineNumber = lineNumber
            };
            
            var componentMessage = LogMessageTransport.Create(logMessage);
            MessageBus.Current.SendMessage(componentMessage);
        }
    }
}