using Avalonia.Controls;
using DockComponent.BlazorHost.ViewModels;
using ReactiveUI;
using Avalonia;
using WebViewControl;
using System.Reactive.Disposables;
using System;

namespace DockComponent.BlazorHost.Views;

public partial class BlazorHostView : UserControl, IViewFor<BlazorHostViewModel>
{
    public BlazorHostView()
    {
        System.Diagnostics.Debug.WriteLine("🏗️ BlazorHostView constructor START");
        
        // Initialize WebView settings BEFORE InitializeComponent
        try
        {
            WebView.Settings.LogFile = "blazor-webview.log";
            System.Diagnostics.Debug.WriteLine("🌐 WebView settings initialized");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ WebView settings init warning: {ex.Message}");
        }
        
        System.Diagnostics.Debug.WriteLine("🏗️ About to call InitializeComponent");
        InitializeComponent();
        System.Diagnostics.Debug.WriteLine("🏗️ InitializeComponent completed");
        
        // Set up ViewModel property change notification
        this.WhenAnyValue(x => x.ViewModel)
            .Subscribe(vm =>
            {
                System.Diagnostics.Debug.WriteLine($"🔗 ViewModel changed to: {vm?.GetType().Name ?? "NULL"}");
                SetupViewModelSubscriptions(vm);
            });
            
        System.Diagnostics.Debug.WriteLine("🏗️ BlazorHostView constructor END");
    }
    
    private void SetupViewModelSubscriptions(BlazorHostViewModel? viewModel)
    {
        if (viewModel == null)
        {
            System.Diagnostics.Debug.WriteLine("⚠️ SetupViewModelSubscriptions: viewModel is null");
            return;
        }
        
        System.Diagnostics.Debug.WriteLine("🔄 Setting up ViewModel subscriptions");
        
        // Subscribe to property changes
        viewModel.WhenAnyValue(x => x.CurrentUrl)
            .Subscribe(url =>
            {
                System.Diagnostics.Debug.WriteLine($"🔄 View: CurrentUrl changed to: '{url}'");
                
                // FORCE WebView to navigate immediately
                if (!string.IsNullOrEmpty(url) && BlazorWebView != null)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"🔧 Forcing WebView.Address = '{url}'");
                        BlazorWebView.Address = url;
                        System.Diagnostics.Debug.WriteLine($"✅ WebView.Address set successfully");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error setting WebView.Address: {ex.Message}");
                    }
                }
            });
            
        viewModel.WhenAnyValue(x => x.IsLoaded)
            .Subscribe(isLoaded =>
            {
                System.Diagnostics.Debug.WriteLine($"🔄 View: IsLoaded changed to: {isLoaded}");
            });
            
        viewModel.WhenAnyValue(x => x.StatusMessage)
            .Subscribe(status =>
            {
                System.Diagnostics.Debug.WriteLine($"🔄 View: StatusMessage changed to: '{status}'");
            });
            
        System.Diagnostics.Debug.WriteLine("✅ All ViewModel subscriptions set up");
    }

    public static readonly StyledProperty<BlazorHostViewModel?> ViewModelProperty =
        AvaloniaProperty.Register<BlazorHostView, BlazorHostViewModel?>(nameof(ViewModel));

    public BlazorHostViewModel? ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = value as BlazorHostViewModel;
    }

}