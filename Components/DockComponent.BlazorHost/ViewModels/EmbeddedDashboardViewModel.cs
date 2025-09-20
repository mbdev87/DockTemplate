using System.Reactive;
using ReactiveUI;
using Dock.Model.Mvvm.Controls;
using DockComponent.BlazorHost.Services;
using FluentBlazorExample.Services;
using ReactiveUI.Fody.Helpers;

namespace DockComponent.BlazorHost.ViewModels;

public class EmbeddedDashboardViewModel : Document, IDisposable
{
    private readonly BlazorServerManager _dashboardServer;
    private readonly IThemeService? _sharedThemeService;
    private readonly IDashboardService? _sharedDashboardService;
    private bool _disposed;

    [Reactive] public string CurrentUrl { get; set; } = string.Empty;
    [Reactive] public bool IsLoaded { get; set; }
    [Reactive] public bool IsServerRunning { get; set; }
    [Reactive] public string StatusMessage { get; set; } = "Dashboard server stopped";

    public EmbeddedDashboardViewModel(
        IThemeService? sharedThemeService = null,
        IDashboardService? sharedDashboardService = null)
    {
        Title = "📊 Embedded Dashboard";
        Id = "EmbeddedDashboard";

        _sharedThemeService = sharedThemeService;
        _sharedDashboardService = sharedDashboardService;
        _dashboardServer = new BlazorServerManager();

        Console.WriteLine("🏗️ [EMBEDDED DASHBOARD] ViewModel created");

        // Wire up server events
        _dashboardServer.ServerStarted += OnServerStarted;
        _dashboardServer.ServerStopped += OnServerStopped;
        _dashboardServer.ServerError += OnServerError;

        // Commands
        StartServerCommand = ReactiveCommand.CreateFromTask(StartServer,
            this.WhenAnyValue(x => x.IsServerRunning, running => !running));

        StopServerCommand = ReactiveCommand.CreateFromTask(StopServer,
            this.WhenAnyValue(x => x.IsServerRunning));

        RefreshCommand = ReactiveCommand.Create(Refresh,
            this.WhenAnyValue(x => x.IsServerRunning));

        // Auto-start the server
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(1000); // Give a moment for DI setup
                await StartServer();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [EMBEDDED DASHBOARD] Auto-start failed: {ex.Message}");
            }
        });
    }

    public ReactiveCommand<Unit, Unit> StartServerCommand { get; }
    public ReactiveCommand<Unit, Unit> StopServerCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    private async Task StartServer()
    {
        if (_disposed || IsServerRunning) return;

        try
        {
            StatusMessage = "🚀 Starting embedded dashboard server...";
            Console.WriteLine("🚀 [EMBEDDED DASHBOARD] Starting server with shared services");

            var url = await _dashboardServer.StartAsync();

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
            Console.WriteLine($"✅ [EMBEDDED DASHBOARD] Server started, dashboard at: {dashboardUrl}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Failed to start server: {ex.Message}";
            Console.WriteLine($"❌ [EMBEDDED DASHBOARD] Start failed: {ex.Message}");
        }
    }

    private async Task StopServer()
    {
        if (_disposed || !IsServerRunning) return;

        try
        {
            StatusMessage = "🛑 Stopping dashboard server...";
            await _dashboardServer.StopAsync();
            CurrentUrl = string.Empty;
            IsLoaded = false;
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Failed to stop server: {ex.Message}";
            Console.WriteLine($"❌ [EMBEDDED DASHBOARD] Stop failed: {ex.Message}");
        }
    }

    private void Refresh()
    {
        if (!IsServerRunning) return;

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

    private void OnServerStarted(string url)
    {
        IsServerRunning = true;
        StatusMessage = $"✅ Dashboard server running on {url}";
        Console.WriteLine($"✅ [EMBEDDED DASHBOARD] Server started: {url}");
    }

    private void OnServerStopped()
    {
        IsServerRunning = false;
        StatusMessage = "🛑 Dashboard server stopped";
        Console.WriteLine("🛑 [EMBEDDED DASHBOARD] Server stopped");
    }

    private void OnServerError(Exception ex)
    {
        StatusMessage = $"❌ Server error: {ex.Message}";
        Console.WriteLine($"❌ [EMBEDDED DASHBOARD] Server error: {ex.Message}");
    }

    public void UpdateTheme()
    {
        if (IsServerRunning)
        {
            Refresh();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _dashboardServer.ServerStarted -= OnServerStarted;
            _dashboardServer.ServerStopped -= OnServerStopped;
            _dashboardServer.ServerError -= OnServerError;

            _dashboardServer.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [EMBEDDED DASHBOARD] Disposal error: {ex.Message}");
        }
    }
}