using Avalonia.Controls;
using DockComponent.BlazorHost.ViewModels;
using WebViewControl;
using ReactiveUI;

namespace DockComponent.BlazorHost.Views;

public partial class FluentDashboardView : UserControl
{
    public FluentDashboardView()
    {
        // Initialize WebView settings BEFORE InitializeComponent
        try
        {
            WebView.Settings.LogFile = "fluent-dashboard-webview.log";
            System.Diagnostics.Debug.WriteLine("🌐 FluentDashboard WebView settings initialized");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ FluentDashboard WebView settings init warning: {ex.Message}");
        }

        InitializeComponent();
        System.Diagnostics.Debug.WriteLine("🎨 FluentDashboardView created");
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is FluentDashboardViewModel viewModel)
        {
            System.Diagnostics.Debug.WriteLine($"🎨 FluentDashboardView bound to ViewModel: {viewModel.Title}");

            // Watch for URL changes and update WebView
            viewModel.WhenAnyValue(x => x.DashboardUrl)
                .Subscribe(url =>
                {
                    if (!string.IsNullOrEmpty(url) && DashboardWebView != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"🎨 Setting FluentDashboard WebView URL: {url}");
                        DashboardWebView.Address = url;
                    }
                });
        }
    }
}