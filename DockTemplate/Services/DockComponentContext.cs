using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Markup.Xaml.Styling;
using DockComponent.Base;
using DockTemplate.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DockTemplate.Services;

public record ComponentRegistration(string Id, object ViewModel, DockPosition Position);

public class DockComponentContext : IDockComponentContext
{
    private readonly DockFactory _dockFactory;
    private readonly List<ComponentRegistration> _registeredTools = new();
    private readonly List<ComponentRegistration> _registeredDocuments = new();

    public DockComponentContext(DockFactory dockFactory, IServiceCollection services)
    {
        _dockFactory = dockFactory;
        Services = services;
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

    public void RegisterTool(string id, object toolViewModel, DockPosition position = DockPosition.Left)
    {
        var registration = new ComponentRegistration(id, toolViewModel, position);
        _registeredTools.Add(registration);
        Console.WriteLine($"[ComponentContext] Registered tool: {id} -> {toolViewModel.GetType().Name} at {position}");
    }

    public void RegisterDocument(string id, object documentViewModel, DockPosition position = DockPosition.Document)
    {
        var registration = new ComponentRegistration(id, documentViewModel, position);
        _registeredDocuments.Add(registration);
        Console.WriteLine($"[ComponentContext] Registered document: {id} -> {documentViewModel.GetType().Name} at {position}");
    }

    public IReadOnlyList<ComponentRegistration> RegisteredTools => _registeredTools.AsReadOnly();
    public IReadOnlyList<ComponentRegistration> RegisteredDocuments => _registeredDocuments.AsReadOnly();
}