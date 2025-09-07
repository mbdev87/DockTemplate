using System;
using DockComponent.Base;
using DockComponent.Editor.ViewModels;
using Dock.Model.Mvvm.Controls;

namespace DockComponent.Editor
{
    public class EditorComponent : IDockComponent
    {
        public string Name => "Editor Component";
        public string Version => "1.0.0";
        public Guid InstanceId { get; } = Guid.NewGuid();

        public void Register(IDockComponentContext context)
        {
            // Load component styles - CRITICAL for Avalonia View discovery!
            var stylesUri = new Uri("avares://DockComponent.Editor/Styles.axaml");
            context.RegisterResources(stylesUri);
            
            // Register the Editor as a document (not tool) - like Dashboard
            context.RegisterDocument("Editor", new EditorToolViewModel());
        }
    }
}