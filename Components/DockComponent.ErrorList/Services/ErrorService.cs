using System.Collections.ObjectModel;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using DockComponent.ErrorList.Messages;
using DockComponent.Base;
using System.Text.Json;
using NLog;

namespace DockComponent.ErrorList.Services
{
    public class ErrorService : ReactiveObject
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ObservableCollection<ErrorEntry> _allErrors = new();
        private readonly ObservableCollection<ErrorEntry> _filteredErrors = new();

        public ObservableCollection<ErrorEntry> Errors => _filteredErrors;
        
        [Reactive] public string FilterText { get; set; } = string.Empty;
        [Reactive] public string SelectedSeverity { get; set; } = "All";

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
                            _allErrors.Insert(0, errorEntry);
                            ApplyFilters();
                            UpdateCounts();
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"❌ Failed to deserialize error entry message: {ex.Message}");
                    }
                });

            // Setup reactive filtering
            this.WhenAnyValue(x => x.FilterText, x => x.SelectedSeverity)
                .Subscribe(_ => ApplyFilters());

            // Log service initialization
            LogInfo("✅ Error Service initialized - listening for error messages from other components");
        }


        private void ApplyFilters()
        {
            _filteredErrors.Clear();
            
            var filtered = _allErrors.AsEnumerable();
            
            // Apply severity filter
            if (SelectedSeverity != "All")
            {
                filtered = filtered.Where(entry => entry.Level == SelectedSeverity);
            }
            
            // Apply text filter
            if (!string.IsNullOrWhiteSpace(FilterText))
            {
                filtered = filtered.Where(entry => 
                    (entry.Message?.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (entry.Source?.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (entry.Code?.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ?? false));
            }
            
            foreach (var entry in filtered)
                _filteredErrors.Add(entry);
        }

        private void UpdateCounts()
        {
            this.RaisePropertyChanged(nameof(ErrorCount));
            this.RaisePropertyChanged(nameof(WarningCount));
        }

        public int ErrorCount => _allErrors.Count(e => e.Level == "Error");
        public int WarningCount => _allErrors.Count(e => e.Level == "Warning");

        public void Clear()
        {
            _allErrors.Clear();
            _filteredErrors.Clear();
            this.RaisePropertyChanged(nameof(ErrorCount));
            this.RaisePropertyChanged(nameof(WarningCount));
        }

        public void AddError(ErrorEntry error)
        {
            _allErrors.Insert(0, error);
            ApplyFilters();
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