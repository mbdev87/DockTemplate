using ReactiveUI;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DockComponent.BlazorHost.Transport.BlazorHostComponent;

namespace DockTemplate.Services;

public class BlazorAutoIntegrationService : ReactiveObject, IDisposable
{
    private readonly FileSystemWatcher _fileWatcher;
    private readonly string _signalFilePath;
    private bool _isDisposed;

    public BlazorAutoIntegrationService()
    {
        _signalFilePath = Path.Combine(Directory.GetCurrentDirectory(), "blazor-app-url.txt");
        var watchDirectory = Directory.GetCurrentDirectory();

        _fileWatcher = new FileSystemWatcher(watchDirectory, "blazor-app-url.txt")
        {
            NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };

        _fileWatcher.Created += OnSignalFileChanged;
        _fileWatcher.Changed += OnSignalFileChanged;

        // Check if file already exists (in case Blazor app started before us)
        _ = Task.Run(CheckExistingSignalFile);
    }

    private async void OnSignalFileChanged(object sender, FileSystemEventArgs e)
    {
        if (_isDisposed) return;

        try
        {
            // Small delay to ensure file write is complete
            await Task.Delay(100);
            await ProcessSignalFile();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error processing signal file: {ex.Message}");
        }
    }

    private async Task CheckExistingSignalFile()
    {
        try
        {
            if (File.Exists(_signalFilePath))
            {
                await ProcessSignalFile();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking existing signal file: {ex.Message}");
        }
    }

    private async Task ProcessSignalFile()
    {
        try
        {
            if (!File.Exists(_signalFilePath)) return;

            var url = await File.ReadAllTextAsync(_signalFilePath);
            if (string.IsNullOrWhiteSpace(url)) return;

            url = url.Trim();

            System.Diagnostics.Debug.WriteLine($"🚀 Auto-detected Blazor app: {url}");

            // Emit message to auto-load URL in WebHost component
            MessageBus.Current.SendMessage(new BlazorAppStartedMsg
            {
                Url = url,
                Port = ExtractPortFromUrl(url)
            });

            // Also emit a specific auto-integration message
            MessageBus.Current.SendMessage(new BlazorAutoIntegrationMsg
            {
                Url = url,
                DetectedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error processing signal file: {ex.Message}");
        }
    }

    private static int ExtractPortFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Port;
        }
        catch
        {
            return 0;
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;
        _fileWatcher?.Dispose();

        // Clean up signal file
        try
        {
            if (File.Exists(_signalFilePath))
            {
                File.Delete(_signalFilePath);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}

// Additional message for auto-integration
public class BlazorAutoIntegrationMsg
{
    public string Url { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}