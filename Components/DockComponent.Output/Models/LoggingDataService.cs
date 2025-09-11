using System.Collections.ObjectModel;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using DockComponent.Output.Messages;
using DockComponent.Base;

namespace DockComponent.Output.Models
{
    public class LoggingDataService : ReactiveObject
    {
        public ObservableCollection<LogEntry> LogEntries { get; } = new();
        public ObservableCollection<LogEntry> FilteredEntries { get; } = new();
        
        [Reactive] public string FilterText { get; set; } = "";
        [Reactive] public string SelectedLogLevel { get; set; } = "All";
        
        public string[] LogLevels { get; } = { "All", "Debug", "Info", "Warn", "Error" };
        
        public LoggingDataService()
        {
            // Connect to SharedLoggingService for centralized, consistent logging
            // This ensures we show ALL logs from host + all components with proper formatting
            ConnectToSharedLogging();
        }
        
        private void ConnectToSharedLogging()
        {
            try
            {
                // Initialize shared logging if not already done
                SharedLoggingService.Instance.ConfigureComponentLogging("OutputComponent");
                
                // Subscribe to shared log entries - this gets ALL logs from everywhere
                SharedLoggingService.Instance.LogEntries.CollectionChanged += (sender, e) =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        // Convert shared log entries to our format and sync collections
                        SyncLogEntries();
                        ApplyFilters();
                    });
                };
                
                // Setup reactive filtering for main path too!
                this.WhenAnyValue(x => x.FilterText, x => x.SelectedLogLevel)
                    .Subscribe(_ => ApplyFilters());
                
                // Initial sync
                SyncLogEntries();
                ApplyFilters();
                
                Console.WriteLine("[OutputComponent] Connected to SharedLoggingService - showing centralized logs");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OutputComponent] Failed to connect to SharedLoggingService: {ex.Message}");
                
                // Fallback to message bus for individual component messages
                SetupFallbackMessageListener();
            }
        }
        
        private void SyncLogEntries()
        {
            // Convert from shared LogEntry format to our local LogEntry format
            var sharedEntries = SharedLoggingService.Instance.LogEntries;
            
            // Clear and repopulate to ensure sync (could be optimized to only add new entries)
            LogEntries.Clear();
            foreach (var sharedEntry in sharedEntries)
            {
                // sharedEntry is already the correct Base.LogEntry type, just add it directly
                LogEntries.Add(sharedEntry);
            }
        }
        
        private void SetupFallbackMessageListener()
        {
            // Fallback: Listen for log messages from other components if SharedLoggingService fails
            ReactiveUI.MessageBus.Current.Listen<ComponentMessage>()
                .Where(msg => msg.Name == LogMessageTransport.MESSAGE_NAME)
                .Subscribe(msg =>
                {
                    var logMessage = LogMessageTransport.Parse(msg);
                    if (logMessage != null)
                    {
                        var logEntry = new LogEntry(
                            logMessage.Level, 
                            logMessage.Message, 
                            logMessage.Source, 
                            logMessage.Timestamp,
                            logMessage.FilePath,
                            logMessage.LineNumber
                        );
                        
                        // Add to main collection
                        LogEntries.Add(logEntry);
                        
                        // Apply current filters
                        ApplyFilters();
                    }
                });
            
            // Add initial component startup message
            var startupEntry = new LogEntry("Info", "✅ Output component initialized - listening for log messages", "Output", DateTime.Now);
            LogEntries.Add(startupEntry);
            FilteredEntries.Add(startupEntry);
                
            // Setup reactive filtering
            this.WhenAnyValue(x => x.FilterText, x => x.SelectedLogLevel)
                .Subscribe(_ => ApplyFilters());
        }
        
        private void ApplyFilters()
        {
            FilteredEntries.Clear();
            
            var filtered = LogEntries.AsEnumerable();
            
            // Apply log level filter
            if (SelectedLogLevel != "All")
            {
                filtered = filtered.Where(entry => entry.Level == SelectedLogLevel);
            }
            
            // Apply text filter
            if (!string.IsNullOrWhiteSpace(FilterText))
            {
                filtered = filtered.Where(entry => 
                    entry.Message.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                    entry.Source.Contains(FilterText, StringComparison.OrdinalIgnoreCase));
            }
            
            foreach (var entry in filtered)
                FilteredEntries.Add(entry);
        }
        
        public void ClearLogs()
        {
            LogEntries.Clear();
            FilteredEntries.Clear();
        }
        
        public void AddLogEntry(LogEntry entry)
        {
            LogEntries.Add(entry);
            ApplyFilters();
        }
    }
}