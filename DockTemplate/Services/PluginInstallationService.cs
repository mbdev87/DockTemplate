using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReactiveUI;
using DockTemplate.Messages;
using DockTemplate.ViewModels;
using NLog;

namespace DockTemplate.Services;

public class PluginInstallationService : BackgroundService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly IServiceProvider _serviceProvider;

    public PluginInstallationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        
        // Subscribe to installation requests
        MessageBus.Current.Listen<InstallPluginMessage>()
            .Subscribe(async message => await HandleInstallPluginRequest(message));
            
        Logger.Info("PluginInstallationService initialized");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.Info("PluginInstallationService started");
        
        // Keep the service running
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
        
        Logger.Info("PluginInstallationService stopped");
    }

    private async Task HandleInstallPluginRequest(InstallPluginMessage message)
    {
        try
        {
            Logger.Info($"Starting plugin installation: {message.PluginFileName}");
            MessageBus.Current.SendMessage(new PluginInstallationStartedMessage(message.PluginFileName));
            await Task.Delay(500);
            await InstallPluginAsync(message.PluginPath);
            await Task.Delay(500);
            MessageBus.Current.SendMessage(new PluginInstallationCompletedMessage(message.PluginFileName, true));
            Logger.Info($"Plugin installation completed: {message.PluginFileName}");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to install plugin: {message.PluginFileName}");
            MessageBus.Current.SendMessage(new PluginInstallationCompletedMessage(
                message.PluginFileName, false, ex.Message));
        }
    }

    private async Task InstallPluginAsync(string pluginPath)
    {
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
    }
    
    private async Task HotLoadNewPluginAsync(string componentsPath)
    {
        try
        {
            Logger.Info("Starting hot-loading of newly installed plugin...");
            
            var componentLoader = _serviceProvider.GetService<ComponentLoader>();
            var dockFactory = _serviceProvider.GetService<DockFactory>();
            var componentContext = _serviceProvider.GetService<DockComponentContext>();
            
            if (componentLoader == null || dockFactory == null || componentContext == null)
            {
                Logger.Error("Required services not available for hot-loading");
                return;
            }
            
            // Load components from the directory (this will detect new ones)
            Logger.Info($"Scanning for new components in: {componentsPath}");
            componentLoader.LoadComponents(componentsPath);
            
            // Update the dock factory with any new components
            var registry = ComponentRegistry.Instance;
            Logger.Info($"Updating dock factory with {registry.LoadedComponents.Count} total components");
            dockFactory.StoreComponents(componentContext.RegisteredTools, componentContext.RegisteredDocuments);
            
            // Send message to UI to refresh layout
            MessageBus.Current.SendMessage(new UILoadedMessage());
            
            Logger.Info("Hot-loading completed! New plugin should be visible.");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to hot-load newly installed plugin");
            throw; // Re-throw so main handler can catch and notify UI
        }
    }
}