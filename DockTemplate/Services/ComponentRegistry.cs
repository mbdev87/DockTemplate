using System;
using System.Collections.Generic;
using System.Linq;
using DockComponent.Base;
using NLog;

namespace DockTemplate.Services;

public record ComponentInfo(string Name, string Version, string AssemblyPath, bool IsEnabled, DateTime LoadedAt);

public class ComponentRegistry
{
    private static readonly Lazy<ComponentRegistry> _instance = new(() => new ComponentRegistry());
    public static ComponentRegistry Instance => _instance.Value;
    
    private readonly List<ComponentRegistration> _componentTools = new();
    private readonly List<ComponentRegistration> _componentDocuments = new();
    private readonly Dictionary<string, ComponentInfo> _loadedComponents = new();
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private ComponentRegistry()
    {
    }

    public bool IsAlreadyLoaded(string name, string version)
    {
        var key = $"{name}:{version}";
        return _loadedComponents.ContainsKey(key);
    }

    public bool TryRegisterComponent(IDockComponent component, string assemblyPath)
    {
        var key = $"{component.Name}:{component.Version}";
        
        if (_loadedComponents.ContainsKey(key))
        {
            Logger.Warn($"[ComponentRegistry] Plugin {component.Name} v{component.Version} already loaded - skipping duplicate");
            return false;
        }
        
        var componentInfo = new ComponentInfo(
            component.Name, 
            component.Version, 
            assemblyPath, 
            IsEnabled: true, 
            DateTime.Now
        );
        
        _loadedComponents[key] = componentInfo;
        Logger.Info($"[ComponentRegistry] Registered new component: {component.Name} v{component.Version}");
        return true;
    }

    public void StoreComponents(IReadOnlyCollection<ComponentRegistration> tools, IReadOnlyCollection<ComponentRegistration> documents)
    {
        // Clear existing registrations to prevent duplicates
        _componentTools.Clear();
        _componentDocuments.Clear();
        
        _componentTools.AddRange(tools);
        _componentDocuments.AddRange(documents);
        
        Logger.Info($"[ComponentRegistry] Stored {tools.Count()} component tools and {documents.Count()} component documents globally (replaced existing)");
    }
    
    public void AddComponents(IEnumerable<ComponentRegistration> tools, IEnumerable<ComponentRegistration> documents)
    {
        foreach (var tool in tools)
        {
            // Check for duplicates by ComponentInstanceId
            if (!_componentTools.Any(t => t.ComponentInstanceId == tool.ComponentInstanceId && t.Id == tool.Id))
            {
                _componentTools.Add(tool);
                Logger.Info($"[ComponentRegistry] Added tool: {tool.Id} (Instance: {tool.ComponentInstanceId})");
            }
            else
            {
                Logger.Info($"[ComponentRegistry] Tool {tool.Id} (Instance: {tool.ComponentInstanceId}) already registered - skipping");
            }
        }
        
        foreach (var document in documents)
        {
            // Check for duplicates by ComponentInstanceId
            if (!_componentDocuments.Any(d => d.ComponentInstanceId == document.ComponentInstanceId && d.Id == document.Id))
            {
                _componentDocuments.Add(document);
                Logger.Info($"[ComponentRegistry] Added document: {document.Id} (Instance: {document.ComponentInstanceId})");
            }
            else
            {
                Logger.Info($"[ComponentRegistry] Document {document.Id} (Instance: {document.ComponentInstanceId}) already registered - skipping");
            }
        }
    }

    public IReadOnlyList<ComponentRegistration> ComponentTools => _componentTools.AsReadOnly();
    public IReadOnlyList<ComponentRegistration> ComponentDocuments => _componentDocuments.AsReadOnly();
    public IReadOnlyDictionary<string, ComponentInfo> LoadedComponents => _loadedComponents.AsReadOnly();

    public void SetComponentEnabled(string name, string version, bool enabled)
    {
        var key = $"{name}:{version}";
        if (_loadedComponents.TryGetValue(key, out var component))
        {
            _loadedComponents[key] = component with { IsEnabled = enabled };
            Logger.Info($"[ComponentRegistry] Set component {name} v{version} enabled: {enabled}");
        }
    }

    public void Clear()
    {
        _componentTools.Clear();
        _componentDocuments.Clear();
        _loadedComponents.Clear();
        Logger.Info("[ComponentRegistry] Cleared all stored components");
    }
}