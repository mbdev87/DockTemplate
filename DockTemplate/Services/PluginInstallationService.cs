using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
        
        // Check if plugin is already installed
        var appDataRoot = Path.GetDirectoryName(localAppDataPath)!;
        var copiedPluginPath = Path.Combine(appDataRoot, pluginFileName);
        
        if (File.Exists(copiedPluginPath))
        {
            Logger.Info($"Plugin {pluginFileName} already installed - checking for component duplicates");
            
            // Check if components from this plugin are already loaded to prevent runtime duplicates
            var tempExtractPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                using (var archive = ZipFile.OpenRead(pluginPath))
                {
                    archive.ExtractToDirectory(tempExtractPath);
                    
                    // Load the plugin to get its component info for duplicate checking
                    var dllFiles = Directory.GetFiles(tempExtractPath, "*.dll", SearchOption.AllDirectories)
                        .Where(f => !Path.GetFileName(f).Equals("DockComponent.Base.dll", StringComparison.OrdinalIgnoreCase));
                        
                    foreach (var dllFile in dllFiles)
                    {
                        try
                        {
                            var assembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(dllFile);
                            var componentTypes = assembly.GetTypes()
                                .Where(t => typeof(DockComponent.Base.IDockComponent).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
                                
                            foreach (var componentType in componentTypes)
                            {
                                var component = (DockComponent.Base.IDockComponent)Activator.CreateInstance(componentType)!;
                                if (ComponentRegistry.Instance.IsAlreadyLoaded(component.Name, component.Version))
                                {
                                    Logger.Warn($"⚠️ Plugin {pluginFileName} contains component {component.Name} v{component.Version} that's already loaded - will skip duplicate");
                                    return; // Don't install/re-process
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Debug(ex, $"Could not check component in {dllFile} for duplicates");
                        }
                    }
                }
            }
            finally
            {
                if (Directory.Exists(tempExtractPath))
                    Directory.Delete(tempExtractPath, true);
            }
            
            Logger.Info($"✅ Plugin {pluginFileName} is already installed but not loaded - proceeding with hot-loading");
        }
        
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
            
            // Detailed diagnostic logging for service availability
            Logger.Info($"Service availability check:");
            Logger.Info($"  ComponentLoader: {(componentLoader != null ? "✅ Available" : "❌ MISSING")}");
            Logger.Info($"  DockFactory: {(dockFactory != null ? "✅ Available" : "❌ MISSING")}");
            Logger.Info($"  DockComponentContext: {(componentContext != null ? "✅ Available" : "❌ MISSING")}");
            
            if (componentLoader == null)
            {
                Logger.Error("❌ ComponentLoader service is not registered in DI container - cannot hot-load plugins");
                return;
            }
            
            if (dockFactory == null)
            {
                Logger.Error("❌ DockFactory service is not registered in DI container - cannot hot-load plugins");
                return;
            }
            
            if (componentContext == null)
            {
                Logger.Error("⚠️ DockComponentContext service is not registered - this is expected with new architecture (components create their own contexts)");
                // Don't return - this is actually expected behavior now since we create contexts per component
            }
            
            // Load components from the directory (this will detect new ones)
            Logger.Info($"Scanning for new components in: {componentsPath}");
            componentLoader.LoadComponents(componentsPath);
            
            // Components register themselves in ComponentRegistry, so get them from there
            var registry = ComponentRegistry.Instance;
            Logger.Info($"After component loading:");
            Logger.Info($"  Total loaded components: {registry.LoadedComponents.Count}");
            Logger.Info($"  Registered tools: {registry.ComponentTools.Count}");  
            Logger.Info($"  Registered documents: {registry.ComponentDocuments.Count}");
            
            // The DockFactory will get components from ComponentRegistry via StoreComponents
            // But since we don't have a shared context anymore, components register directly
            // So we need to trigger integration manually
            Logger.Info("Triggering component integration...");
            dockFactory.IntegrateComponentsAfterUILoad();
            
            Logger.Info("✅ Hot-loading completed! New plugin should now be visible in UI.");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to hot-load newly installed plugin");
            throw; // Re-throw so main handler can catch and notify UI
        }
    }
}