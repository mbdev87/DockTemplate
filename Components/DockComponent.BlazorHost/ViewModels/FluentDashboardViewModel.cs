using ReactiveUI;
using Dock.Model.Mvvm.Controls;
using ReactiveUI.Fody.Helpers;
using FluentBlazorExample.Services;

namespace DockComponent.BlazorHost.ViewModels;

public class FluentDashboardViewModel : Document, IDisposable
{
    [Reactive] public bool IsLoaded { get; set; } = true;
    [Reactive] public string StatusMessage { get; set; } = "Loading FluentBlazor Dashboard...";
    [Reactive] public string DashboardUrl { get; set; } = string.Empty;

    private readonly IThemeService? _themeService;
    private readonly IDashboardService? _dashboardService;

    public FluentDashboardViewModel(IThemeService? themeService = null, IDashboardService? dashboardService = null)
    {
        Title = "🎨 FluentBlazor Dashboard";
        Id = "FluentDashboard";

        _themeService = themeService;
        _dashboardService = dashboardService;

        System.Diagnostics.Debug.WriteLine("🎨 FluentDashboardViewModel created - starting embedded dashboard server");

        // Start the embedded server and get its URL
        _ = StartDashboardServerAsync();
    }

    private async Task StartDashboardServerAsync()
    {
        try
        {
            StatusMessage = "🔄 Starting FluentBlazor server...";

            // Wait for the BlazorServerHost to write the URL file
            var urlFile = Path.Combine(Path.GetTempPath(), "blazor-app-url.txt");
            var maxWait = DateTime.Now.AddSeconds(10);

            while (DateTime.Now < maxWait)
            {
                if (File.Exists(urlFile))
                {
                    var baseUrl = await File.ReadAllTextAsync(urlFile);
                    var dashboardUrl = $"{baseUrl.TrimEnd('/')}/dashboard-embedded";

                    DashboardUrl = dashboardUrl;
                    StatusMessage = "✅ FluentBlazor Dashboard Ready";
                    IsLoaded = true;

                    System.Diagnostics.Debug.WriteLine($"🎨 FluentDashboard WebView URL: {dashboardUrl}");
                    return;
                }

                await Task.Delay(200);
            }

            // Fallback if server file not found
            var fallbackUrl = "http://localhost:5000/dashboard-embedded";
            DashboardUrl = fallbackUrl;
            StatusMessage = "⚠️ Using fallback URL";

            System.Diagnostics.Debug.WriteLine($"🎨 Setting FluentDashboard WebView URL: {fallbackUrl}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Failed to load dashboard: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"❌ FluentDashboard error: {ex.Message}");
        }
    }

    public IThemeService? ThemeService => _themeService;
    public IDashboardService? DashboardService => _dashboardService;

    public void Dispose()
    {
        // No cleanup needed - server is managed by BlazorServerHost service
    }
}