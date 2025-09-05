using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using DockComponent.Base;
using NLog;

namespace DockTemplate.Services;

public class ComponentLoader
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly DockComponentContext _context;
    private readonly List<IDockComponent> _loadedComponents = new();

    public ComponentLoader(DockComponentContext context)
    {
        _context = context;
    }

    public void LoadComponents(string componentDirectory)
    {
        LoadComponents(componentDirectory, _context);
    }

    public void LoadComponents(string componentDirectory, DockComponentContext context)
    {
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
                LoadComponentFromAssembly(dllFile, context);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to load component from: {dllFile}");
            }
        }

        Logger.Info($"Loaded {_loadedComponents.Count} components successfully");
    }

    private void LoadComponentFromAssembly(string dllPath)
    {
        LoadComponentFromAssembly(dllPath, _context);
    }

    private void LoadComponentFromAssembly(string dllPath, DockComponentContext context)
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
                
                Logger.Info($"Registering component: {component.Name} v{component.Version}");
                component.Register(context);
                
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