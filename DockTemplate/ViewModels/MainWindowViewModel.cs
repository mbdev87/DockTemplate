using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Dock.Model.Controls;
using Dock.Model.Core;
using DockTemplate.Services;
using DockTemplate.Messages;
using NLog;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia;

namespace DockTemplate.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IFactory? _factory;
    
    [Reactive] public IRootDock? Layout { get; set; }
    [Reactive] public bool ShowDropOverlay { get; set; } = false;
    [Reactive] public bool ShowSpinner { get; set; } = false;
    [Reactive] public bool IsInstallMode { get; set; } = false;
    [Reactive] public string InstallStatusText { get; set; } = "Installing plugin...";
    [Reactive] public string InstallSubText { get; set; } = "Please wait while we process your plugin";
    [Reactive] public bool IsAcrylicEnabled { get; set; } = true;
    
    // Note: Window is always acrylic-capable, we control the effect through content layering
    
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public ICommand NewLayout { get; }
    public ICommand ShowPluginManager { get; }
    public ICommand InstallPlugin { get; }
    public ICommand ReloadPlugins { get; }
    public ICommand ToggleAcrylic { get; }

    public MainWindowViewModel(DockFactory dockFactory)
    {
        _factory = dockFactory;

        DebugFactoryEvents(_factory);

        var layout = _factory?.CreateLayout();
        if (layout is not null)
        {
            Logger.Info("Init layout");
            _factory?.InitLayout(layout);
            
            // Fire UILoadedMessage after layout is fully initialized
            Logger.Info("UI fully loaded - sending UILoadedMessage");
            MessageBus.Current.SendMessage(new UILoadedMessage());
        }
        Layout = layout;

        // Layout is ready to use directly

        NewLayout = ReactiveCommand.Create(ResetLayout);
        ShowPluginManager = ReactiveCommand.Create(OpenPluginManager);
        InstallPlugin = ReactiveCommand.Create(OpenInstallPluginDialog);
        ReloadPlugins = ReactiveCommand.Create(ReloadAllPlugins);
        ToggleAcrylic = ReactiveCommand.Create(() => 
        {
            IsAcrylicEnabled = !IsAcrylicEnabled;
        });
        
        // Subscribe to plugin installation messages
        MessageBus.Current.Listen<PluginInstallationStartedMessage>()
            .Subscribe(OnPluginInstallationStarted);
            
        MessageBus.Current.Listen<PluginInstallationCompletedMessage>()
            .Subscribe(OnPluginInstallationCompleted);
    }

    public void InitLayout()
    {
        if (Layout is null)
        {
            return;
        }

        _factory?.InitLayout(Layout);
    }

    public void CloseLayout()
    {
        if (Layout is IDock dock)
        {
            if (dock.Close.CanExecute(null))
            {
                dock.Close.Execute(null);
            }
        }
    }

    public void ResetLayout()
    {
        if (Layout is not null)
        {
            if (Layout.Close.CanExecute(null))
            {
                Layout.Close.Execute(null);
            }
        }

        var layout = _factory?.CreateLayout();
        if (layout is not null)
        {
            _factory?.InitLayout(layout);
            Layout = layout;
            
            // Fire UILoadedMessage after layout is reset and fully initialized
            Logger.Info("UI reset complete - sending UILoadedMessage");
            MessageBus.Current.SendMessage(new UILoadedMessage());
        }
    }

    private void DebugFactoryEvents(IFactory factory)
    {
        factory.ActiveDockableChanged += (_, args) =>
        {
            Debug.WriteLine($"[ActiveDockableChanged] Title='{args.Dockable?.Title}'");
        };

        factory.FocusedDockableChanged += (_, args) =>
        {
            Debug.WriteLine($"[FocusedDockableChanged] Title='{args.Dockable?.Title}'");
        };

        factory.DockableAdded += (_, args) =>
        {
            Debug.WriteLine($"[DockableAdded] Title='{args.Dockable?.Title}'");
        };

        factory.DockableRemoved += (_, args) =>
        {
            Debug.WriteLine($"[DockableRemoved] Title='{args.Dockable?.Title}'");
        };
    }

    private void OpenPluginManager()
    {
        Logger.Info("Opening Plugin Manager...");
        
        // For now, just log the loaded plugins - we'll create a proper UI later
        var registry = Services.ComponentRegistry.Instance;
        Logger.Info($"=== Plugin Manager ===");
        Logger.Info($"Total plugins loaded: {registry.LoadedComponents.Count}");
        
        foreach (var (key, component) in registry.LoadedComponents)
        {
            var status = component.IsEnabled ? "✅ Enabled" : "❌ Disabled";
            Logger.Info($"  {component.Name} v{component.Version} - {status}");
            Logger.Info($"    Path: {component.AssemblyPath}");
            Logger.Info($"    Loaded: {component.LoadedAt:yyyy-MM-dd HH:mm:ss}");
        }
        
        // TODO: Show actual Plugin Manager window
    }

    private void OpenInstallPluginDialog()
    {
        Logger.Info("Opening Install Plugin dialog...");
        // TODO: Open file dialog for .dockplugin files
        // TODO: Support drag & drop installation
    }

    private void ReloadAllPlugins()
    {
        Logger.Info("Reloading all plugins...");
        
        // Clear registry
        Services.ComponentRegistry.Instance.Clear();
        
        // Reset layout to trigger plugin reload
        ResetLayout();
        
        Logger.Info("Plugin reload completed");
    }
    
    private void OnPluginInstallationStarted(PluginInstallationStartedMessage message)
    {
        Logger.Info($"Plugin installation started: {message.PluginFileName}");
        // Switch to install mode and start spinner
        IsInstallMode = true;
        ShowSpinner = true;
        InstallStatusText = "Installing plugin...";
        InstallSubText = $"Processing {message.PluginFileName}";
    }
    
    private void OnPluginInstallationCompleted(PluginInstallationCompletedMessage message)
    {
        if (message.Success)
        {
            Logger.Info($"✅ Plugin installation completed: {message.PluginFileName}");
            
            // Just fade out while still spinning - no success message flash
            ShowDropOverlay = false;
            
            // Reset state after fade-out animation completes (300ms)
            Task.Delay(300).ContinueWith(_ =>
            {
                IsInstallMode = false;
                ShowSpinner = false;
                InstallStatusText = "Installing plugin...";
                InstallSubText = "Please wait while we process your plugin";
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        else
        {
            Logger.Error($"❌ Plugin installation failed: {message.PluginFileName} - {message.ErrorMessage}");
            
            ShowSpinner = false;
            InstallStatusText = "❌ Installation failed";
            InstallSubText = message.ErrorMessage ?? "Please try again";
            
            // Show error for a bit longer so user can read it
            Task.Delay(2000).ContinueWith(_ =>
            {
                ShowDropOverlay = false;
                // Reset state after fade-out animation completes
                Task.Delay(300).ContinueWith(__ =>
                {
                    IsInstallMode = false;
                    ShowSpinner = false;
                    InstallStatusText = "Installing plugin...";
                    InstallSubText = "Please wait while we process your plugin";
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}