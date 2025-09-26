using System.Reactive;
using ReactiveUI;
using Dock.Model.Mvvm.Controls;
using FluentBlazorExample.Services;
using ReactiveUI.Fody.Helpers;

namespace DockComponent.BlazorHost.ViewModels;

public class EmbeddedDashboardViewModel : Document, IDisposable
{
    private readonly IThemeService? _sharedThemeService;
    private readonly IDashboardService? _sharedDashboardService;
    private bool _disposed;

    [Reactive] public string CurrentUrl { get; set; } = string.Empty;
    [Reactive] public bool IsLoaded { get; set; }
    [Reactive] public bool IsServerRunning { get; set; } = true; // Server managed by BlazorServerHost
    [Reactive] public string StatusMessage { get; set; } = "Dashboard ready";

    public EmbeddedDashboardViewModel(
        IThemeService? sharedThemeService = null,
        IDashboardService? sharedDashboardService = null)
    {
        Title = "📊 Embedded Dashboard";
        Id = "EmbeddedDashboard";

        _sharedThemeService = sharedThemeService;
        _sharedDashboardService = sharedDashboardService;

        Console.WriteLine("🏗️ [EMBEDDED DASHBOARD] ViewModel created");

        // Commands
        RefreshCommand = ReactiveCommand.Create(Refresh);

        // Load dashboard immediately - server is managed by BlazorServerHost
        LoadDashboard();
    }

    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    private void LoadDashboard()
    {
        if (_disposed) return;

        try
        {
            StatusMessage = "✅ Loading dashboard...";
            Console.WriteLine("🚀 [EMBEDDED DASHBOARD] Loading dashboard from centralized server");

            var url = "http://localhost:5000/dashboard-embedded";

            // Navigate to the real FluentBlazorExample app
            var dashboardUrl = url;

            // Add theme and accent parameters if available
            if (_sharedThemeService != null)
            {
                var theme = _sharedThemeService.Mode.ToString().ToLowerInvariant();
                var accent = _sharedThemeService.OfficeColor.ToString().ToLowerInvariant();
                dashboardUrl += $"?theme={theme}&accent={accent}";
            }

            CurrentUrl = dashboardUrl;
            IsLoaded = true;
            StatusMessage = "✅ Dashboard ready";
            Console.WriteLine($"✅ [EMBEDDED DASHBOARD] Dashboard loaded: {dashboardUrl}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Failed to load dashboard: {ex.Message}";
            Console.WriteLine($"❌ [EMBEDDED DASHBOARD] Load failed: {ex.Message}");
        }
    }

    private void Refresh()
    {
        if (_disposed) return;

        try
        {
            // Update URL with current theme settings
            if (_sharedThemeService != null && !string.IsNullOrEmpty(CurrentUrl))
            {
                var baseUrl = CurrentUrl.Split('?')[0];
                var theme = _sharedThemeService.Mode.ToString().ToLowerInvariant();
                var accent = _sharedThemeService.OfficeColor.ToString().ToLowerInvariant();
                CurrentUrl = $"{baseUrl}?theme={theme}&accent={accent}&refresh={DateTime.Now.Ticks}";

                StatusMessage = $"🔄 Refreshed with theme: {theme}, accent: {accent}";
                Console.WriteLine($"🔄 [EMBEDDED DASHBOARD] Refreshed URL: {CurrentUrl}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Refresh failed: {ex.Message}";
        }
    }

    public void UpdateTheme()
    {
        if (!_disposed)
        {
            Refresh();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // No server cleanup needed - managed by BlazorServerHost service
        Console.WriteLine("🛑 [EMBEDDED DASHBOARD] ViewModel disposed");
    }
}