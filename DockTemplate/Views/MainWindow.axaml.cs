using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using DockTemplate.Services;
using DockTemplate.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace DockTemplate.Views;

public partial class MainWindow : Window
{
    private bool _isDark = false;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public MainWindow()
    {
        InitializeComponent();
        InitializeThemes();
        InitializeDragDrop();
    }

    private void InitializeThemes()
    {
        if (ThemeButton is not null)
        {
            ThemeButton.Click += (_, _) =>
            {
                _isDark = !_isDark;
                App.ThemeService?.Switch(_isDark ? 1 : 0);
            };
        }
    }

    private void InitializeDragDrop()
    {
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (IsPluginFile(e))
        {
            e.DragEffects = DragDropEffects.Copy;
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.ShowDropOverlay = true;
            }
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ShowDropOverlay = false;
        }
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ShowDropOverlay = false;
        }

        if (!IsPluginFile(e))
            return;

        var files = e.Data.GetFiles()?.Select(f => f.Path.LocalPath).Where(f => !string.IsNullOrEmpty(f)).ToArray();
        if (files == null || !files.Any())
            return;

        foreach (var file in files)
        {
            if (IsPluginFile(file))
            {
                Console.WriteLine($"[MainWindow] Installing plugin: {Path.GetFileName(file)}");
                // TODO: Install plugin
                await InstallPluginAsync(file);
            }
        }
    }

    private bool IsPluginFile(DragEventArgs e)
    {
        var files = e.Data.GetFiles()?.Select(f => f.Path.LocalPath).Where(f => !string.IsNullOrEmpty(f)).ToArray();
        return files?.Any(IsPluginFile) == true;
    }

    private bool IsPluginFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension == ".dockplugin" || extension == ".zip";
    }

    private async Task InstallPluginAsync(string pluginPath)
    {
        try
        {
            Logger.Info($"Installing plugin from {pluginPath}");
            
            var pluginFileName = Path.GetFileName(pluginPath);
            var localAppDataPath = PluginDirectoryService.GetLocalAppDataPluginPath();
            var componentsPath = localAppDataPath;
            
            // Ensure directories exist
            PluginDirectoryService.EnsureLocalAppDataDirectoryExists();
            
            // Copy the .dockplugin file to LocalAppData root (alongside Components folder)
            var appDataRoot = Path.GetDirectoryName(localAppDataPath)!;
            var copiedPluginPath = Path.Combine(appDataRoot, pluginFileName);
            
            Logger.Info($"Copying {pluginFileName} to {copiedPluginPath}");
            File.Copy(pluginPath, copiedPluginPath, overwrite: true);
            
            // Extract ZIP contents to Components folder
            Logger.Info($"Extracting plugin to {componentsPath}");
            using (var archive = ZipFile.OpenRead(copiedPluginPath))
            {
                foreach (var entry in archive.Entries)
                {
                    // Skip directories
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;
                    
                    var destinationPath = Path.Combine(componentsPath, entry.FullName);
                    var destinationDir = Path.GetDirectoryName(destinationPath)!;
                    
                    // Create directory if it doesn't exist
                    if (!Directory.Exists(destinationDir))
                    {
                        Directory.CreateDirectory(destinationDir);
                    }
                    
                    // Extract file
                    Logger.Info($"  Extracting: {entry.FullName}");
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }
            
            Logger.Info("Plugin extraction completed - starting hot-loading...");
            
            // Hot-load the new plugin immediately!
            await HotLoadNewPluginAsync(componentsPath);
            
            Logger.Info($"Plugin {pluginFileName} installed and loaded successfully!");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to install plugin from {pluginPath}");
        }
    }
    
    private Task HotLoadNewPluginAsync(string componentsPath)
    {
        try
        {
            Logger.Info("Starting hot-loading of newly installed plugin...");
            
            // Get the component loader from the service provider (we need to access DI container)
            var app = (App)App.Current!;
            var serviceProvider = app.GetServiceProvider();
            
            if (serviceProvider == null)
            {
                Logger.Error("Service provider not available for hot-loading");
                return Task.CompletedTask;
            }
            
            var componentLoader = serviceProvider.GetService(typeof(ComponentLoader)) as ComponentLoader;
            var dockFactory = serviceProvider.GetService(typeof(DockFactory)) as DockFactory;
            var componentContext = serviceProvider.GetService(typeof(DockComponentContext)) as DockComponentContext;
            
            if (componentLoader == null || dockFactory == null || componentContext == null)
            {
                Logger.Error("Required services not available for hot-loading");
                return Task.CompletedTask;
            }
            
            // Load components from the directory (this will detect new ones)
            Logger.Info($"Scanning for new components in: {componentsPath}");
            componentLoader.LoadComponents(componentsPath);
            
            // Update the dock factory with any new components
            var registry = ComponentRegistry.Instance;
            Logger.Info($"Updating dock factory with {registry.LoadedComponents.Count} total components");
            dockFactory.StoreComponents(componentContext.RegisteredTools, componentContext.RegisteredDocuments);
            
            // Refresh the layout to show new components
            if (DataContext is MainWindowViewModel viewModel)
            {
                Logger.Info("Refreshing layout to display new components...");
                
                // This will trigger a layout refresh and the new components should appear
                viewModel.ResetLayout();
                
                Logger.Info("Hot-loading completed! New plugin should be visible.");
            }
            else
            {
                Logger.Warn("Could not refresh layout - MainWindowViewModel not found");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to hot-load newly installed plugin");
        }
        
        return Task.CompletedTask;
    }
}