using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Markup.Xaml.Styling;
using DockComponent.Base;
using DockTemplate.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DockTemplate.Services;

public record ComponentRegistration(string Id, object ViewModel, DockPosition Position, Guid ComponentInstanceId, bool IsPrimary = false);

public class DockComponentContext : IDockComponentContext
{
    private readonly DockFactory _dockFactory;
    private readonly List<ComponentRegistration> _registeredTools = new();
    private readonly List<ComponentRegistration> _registeredDocuments = new();
    private readonly Guid _componentInstanceId;

    public DockComponentContext(DockFactory dockFactory, IServiceCollection services, Guid componentInstanceId)
    {
        _dockFactory = dockFactory;
        Services = services;
        _componentInstanceId = componentInstanceId;
        
        // Inject shared host services for components to use (optional)
        InjectSharedHostServices();
    }
    
    /// <summary>
    /// Inject shared services from the host application into component DI container.
    /// Components can choose to use these or provide their own implementations.
    /// This prevents split-brain issues with logging, themes, etc.
    /// </summary>
    private void InjectSharedHostServices()
    {
        try
        {
            // Inject host's service provider if available to access shared services
            var hostServiceProvider = Program.ServiceProvider;
            if (hostServiceProvider != null)
            {
                // Provide the host's theme service for consistent theming
                var themeService = hostServiceProvider.GetService<IThemeService>();
                if (themeService != null)
                {
                    Services.AddSingleton(themeService);
                }
                
                // Provide the host's logging service for consistent logging
                var loggingService = hostServiceProvider.GetService<LoggingService>();
                if (loggingService != null)
                {
                    Services.AddSingleton(loggingService);
                }
                
                // Provide shared logging infrastructure if needed
                // Components can still use their own NLog loggers, but can access centralized services
                var interPluginLogger = hostServiceProvider.GetService<InterPluginLogger>();
                if (interPluginLogger != null)
                {
                    Services.AddSingleton(interPluginLogger);
                }
                
                Console.WriteLine($"[DockComponentContext] Injected shared host services into component {_componentInstanceId}");
            }
            
            // CRITICAL: Always provide SharedLoggingService to ensure consistent logging across components
            Services.AddSingleton<ISharedLoggingService>(provider => SharedLoggingService.Instance);
            
            // Initialize shared logging for this component
            SharedLoggingService.Instance.ConfigureComponentLogging($"Component-{_componentInstanceId}");
            
        }
        catch (Exception ex)
        {
            // Don't fail component registration if shared service injection fails
            Console.WriteLine($"[DockComponentContext] Failed to inject shared services: {ex.Message}");
        }
    }

    public IServiceCollection Services { get; }

    public void RegisterResources(Uri resourceUri)
    {
        try
        {
            var styleInclude = new StyleInclude((Uri?)null)
            {
                Source = resourceUri
            };
            Application.Current?.Styles.Add(styleInclude);
            Console.WriteLine($"[ComponentContext] Loaded styles from: {resourceUri}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ComponentContext] Failed to load styles from {resourceUri}: {ex.Message}");
        }
    }

    public void RegisterTool(string id, object toolViewModel, DockPosition position = DockPosition.Left, bool isPrimary = false)
    {
        var registration = new ComponentRegistration(id, toolViewModel, position, _componentInstanceId, isPrimary);
        _registeredTools.Add(registration);
        
        // CRITICAL: Also store in global ComponentRegistry so DockFactory can find it
        ComponentRegistry.Instance.AddComponents(new[] { registration }, Array.Empty<ComponentRegistration>());
        
        var primaryStatus = isPrimary ? " [PRIMARY]" : "";
        Console.WriteLine($"[ComponentContext] Registered tool: {id} -> {toolViewModel.GetType().Name} at {position}{primaryStatus} (Instance: {_componentInstanceId}) -> Added to global registry");
    }

    public void RegisterDocument(string id, object documentViewModel, DockPosition position = DockPosition.Document)
    {
        var registration = new ComponentRegistration(id, documentViewModel, position, _componentInstanceId);
        _registeredDocuments.Add(registration);
        
        // CRITICAL: Also store in global ComponentRegistry so DockFactory can find it  
        ComponentRegistry.Instance.AddComponents(Array.Empty<ComponentRegistration>(), new[] { registration });
        
        Console.WriteLine($"[ComponentContext] Registered document: {id} -> {documentViewModel.GetType().Name} at {position} (Instance: {_componentInstanceId}) -> Added to global registry");
    }

    public IReadOnlyList<ComponentRegistration> RegisteredTools => _registeredTools.AsReadOnly();
    public IReadOnlyList<ComponentRegistration> RegisteredDocuments => _registeredDocuments.AsReadOnly();
}