using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using DockComponent.Base;
using DockTemplate.ViewModels;
using NLog;

namespace DockTemplate.Services;

public class ComponentLoader
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly DockFactory _dockFactory;
    private readonly Microsoft.Extensions.DependencyInjection.IServiceCollection _services;
    private readonly List<IDockComponent> _loadedComponents = new();

    public ComponentLoader(DockFactory dockFactory, Microsoft.Extensions.DependencyInjection.IServiceCollection services)
    {
        _dockFactory = dockFactory;
        _services = services;
    }

    public void LoadComponents(string componentDirectory)
    {
        // First, scan already loaded assemblies for source-referenced components
        LoadComponentsFromLoadedAssemblies();
        
        // Then scan plugin directories if they exist
        if (!Directory.Exists(componentDirectory))
        {
            Logger.Info($"Component directory does not exist: {componentDirectory}");
            return;
        }

        Logger.Info($"Loading components from: {componentDirectory}");

        foreach (var dllFile in Directory.GetFiles(componentDirectory, "*.dll", SearchOption.AllDirectories))
        {
            try
            {
                LoadComponentFromAssembly(dllFile);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to load component from: {dllFile}");
            }
        }

        Logger.Info($"Loaded {_loadedComponents.Count} components successfully");
    }

    private void LoadComponentsFromLoadedAssemblies()
    {
        Logger.Info("Scanning loaded assemblies for source-referenced components...");
        
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                // Skip system assemblies
                if (assembly.FullName?.StartsWith("System") == true || 
                    assembly.FullName?.StartsWith("Microsoft") == true ||
                    assembly.FullName?.StartsWith("Avalonia") == true ||
                    assembly.FullName?.StartsWith("Dock") == true)
                    continue;

                var componentTypes = assembly.GetTypes()
                    .Where(t => typeof(IDockComponent).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                if (!componentTypes.Any())
                    continue;

                Logger.Info($"Found component types in assembly: {assembly.GetName().Name}");

                foreach (var componentType in componentTypes)
                {
                    try
                    {
                        var component = (IDockComponent)Activator.CreateInstance(componentType)!;
                        
                        // Check for duplicates using ComponentRegistry
                        if (!ComponentRegistry.Instance.TryRegisterComponent(component, assembly.Location))
                        {
                            // Component already loaded - skip this one
                            continue;
                        }
                        
                        Logger.Info($"Registering component: {component.Name} v{component.Version} (Instance: {component.InstanceId})");
                        
                        // Create a unique context for this component instance to prevent duplicates
                        var componentContext = new DockComponentContext(_dockFactory, _services, component.InstanceId);
                        component.Register(componentContext);
                        
                        _loadedComponents.Add(component);
                        Logger.Info($"Successfully loaded component: {component.Name}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"Failed to instantiate component: {componentType.FullName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, $"Could not scan assembly: {assembly.FullName}");
            }
        }
    }

    private void LoadComponentFromAssembly(string dllPath)
    {
        Logger.Debug($"Attempting to load component from: {dllPath}");

        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(dllPath);
        var componentTypes = assembly.GetTypes()
            .Where(t => typeof(IDockComponent).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        if (!componentTypes.Any())
        {
            Logger.Debug($"No IDockComponent implementations found in: {Path.GetFileName(dllPath)}");
            return;
        }

        foreach (var componentType in componentTypes)
        {
            try
            {
                var component = (IDockComponent)Activator.CreateInstance(componentType)!;
                
                // Check for duplicates using ComponentRegistry
                if (!ComponentRegistry.Instance.TryRegisterComponent(component, dllPath))
                {
                    // Component already loaded - skip this one
                    continue;
                }
                
                Logger.Info($"Registering component: {component.Name} v{component.Version} (Instance: {component.InstanceId})");
                
                // Create a unique context for this component instance to prevent duplicates
                var componentContext = new DockComponentContext(_dockFactory, _services, component.InstanceId);
                component.Register(componentContext);
                
                _loadedComponents.Add(component);
                Logger.Info($"Successfully loaded component: {component.Name}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to instantiate component: {componentType.FullName}");
            }
        }
    }

    public IReadOnlyList<IDockComponent> LoadedComponents => _loadedComponents.AsReadOnly();
}