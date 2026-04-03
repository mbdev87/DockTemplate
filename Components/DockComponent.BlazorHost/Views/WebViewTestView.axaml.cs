using Avalonia.Controls;
using WebViewControl;
using System;

namespace DockComponent.BlazorHost.Views;

public partial class WebViewTestView : UserControl
{
    public WebViewTestView()
    {
        System.Diagnostics.Debug.WriteLine(
            "🧪 WebViewTestControl constructor START");

        // Initialize WebView settings
        try
        {
            WebView.Settings.LogFile = "webview-test.log";
            System.Diagnostics.Debug.WriteLine(
                "🌐 WebView settings initialized for test control");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"⚠️ WebView settings init warning: {ex.Message}");
        }

        InitializeComponent();

        System.Diagnostics.Debug.WriteLine(
            "🧪 WebViewTestControl constructor END");
    }
}