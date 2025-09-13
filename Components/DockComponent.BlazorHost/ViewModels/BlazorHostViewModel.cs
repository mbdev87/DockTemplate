using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using Dock.Model.Mvvm.Controls;
using DockComponent.BlazorHost.Services;
using DockComponent.BlazorHost.Transport.BlazorHostComponent;
using ReactiveUI.Fody.Helpers;
using Avalonia.Input.Platform;

namespace DockComponent.BlazorHost.ViewModels;

public class BlazorHostViewModel : Document, IDisposable
{
    [Reactive] public string CurrentUrl { get; set; } = string.Empty;
    [Reactive] public bool IsLoaded { get; set; }
    [Reactive] public string StatusMessage { get; set; } = "No URL loaded";

    public BlazorHostViewModel()
    {
        Title = "🌐 Web Host";
        Id = "BlazorHost";
        
        System.Diagnostics.Debug.WriteLine("🏗️ BlazorHostViewModel constructor called");

        // Commands
        LoadUrlCommand = ReactiveCommand.Create<string>(LoadUrl);
        ClearCommand = ReactiveCommand.Create(Clear, this.WhenAnyValue(x => x.IsLoaded));
        CopyUrlCommand = ReactiveCommand.Create(CopyUrl, this.WhenAnyValue(x => x.IsLoaded));
        OpenInBrowserCommand = ReactiveCommand.Create(OpenInBrowser, this.WhenAnyValue(x => x.IsLoaded));
        
        System.Diagnostics.Debug.WriteLine("🏗️ BlazorHostViewModel commands created");

        // Listen for auto-integration messages from BlazorAutoIntegrationService
        MessageBus.Current.Listen<BlazorAppStartedMsg>()
            .Subscribe(msg =>
            {
                if (!string.IsNullOrEmpty(msg.Url))
                {
                    LoadUrl(msg.Url);
                    StatusMessage = $"🚀 Auto-loaded: {msg.Url}";
                }
            });
    }

    public ReactiveCommand<string, Unit> LoadUrlCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearCommand { get; }
    public ReactiveCommand<Unit, Unit> CopyUrlCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenInBrowserCommand { get; }

    private void LoadUrl(string url)
    {
        System.Diagnostics.Debug.WriteLine($"🚀 LoadUrl called with: '{url}'");
        try
        {
            if (string.IsNullOrWhiteSpace(url)) 
            {
                System.Diagnostics.Debug.WriteLine("❌ LoadUrl: URL is null/empty");
                return;
            }

            // Ensure URL has protocol
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "http://" + url;
                System.Diagnostics.Debug.WriteLine($"🔧 LoadUrl: Added protocol, now: '{url}'");
            }

            System.Diagnostics.Debug.WriteLine($"📝 LoadUrl: About to set properties - CurrentUrl='{CurrentUrl}' → '{url}', IsLoaded={IsLoaded} → true");

            // Update properties directly - should trigger ReactiveUI notifications
            CurrentUrl = url;
            IsLoaded = true;
            StatusMessage = $"✅ Loading: {url}";
            
            System.Diagnostics.Debug.WriteLine($"✅ LoadUrl: Properties set - CurrentUrl='{CurrentUrl}', IsLoaded={IsLoaded}, StatusMessage='{StatusMessage}'");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"💥 LoadUrl exception: {ex}");
            StatusMessage = $"❌ Error loading: {ex.Message}";
        }
    }

    private void Clear()
    {
        try
        {
            // Clear properties directly 
            CurrentUrl = string.Empty;
            IsLoaded = false;
            StatusMessage = "🗑️ Cleared";
            
            // Emit message that content was cleared
            MessageBus.Current.SendMessage(new BlazorAppStoppedMsg 
            { 
                Port = 0 
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error clearing: {ex.Message}";
        }
    }

    private void CopyUrl()
    {
        try
        {
            if (!string.IsNullOrEmpty(CurrentUrl))
            {
                var topLevel = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow : null;
                var clipboard = topLevel?.Clipboard;
                clipboard?.SetTextAsync(CurrentUrl);
                StatusMessage = "📋 URL copied to clipboard!";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Failed to copy: {ex.Message}";
        }
    }

    private void OpenInBrowser()
    {
        try
        {
            if (!string.IsNullOrEmpty(CurrentUrl))
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = CurrentUrl,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
                StatusMessage = "🌐 Opened in default browser";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Failed to open browser: {ex.Message}";
        }
    }

    public void Dispose()
    {
        // Nothing to dispose for now
    }
}