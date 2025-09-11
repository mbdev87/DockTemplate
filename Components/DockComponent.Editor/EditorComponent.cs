using DockComponent.Base;
using DockComponent.Editor.ViewModels.Documents;
using DockComponent.Editor.Services;

namespace DockComponent.Editor
{
    public class EditorComponent : IDockComponent
    {
        private static TextMateService? _textMateService;
        
        public string Name => "Editor Component";
        public string Version => "1.0.0";
        public Guid InstanceId { get; } = Guid.NewGuid();

        public void Register(IDockComponentContext context)
        {
            // Initialize shared TextMateService for syntax highlighting
            _textMateService = new TextMateService();
            
            // Load component styles - CRITICAL for Avalonia View discovery!
            var stylesUri = new Uri("avares://DockComponent.Editor/Styles.axaml");
            context.RegisterResources(stylesUri);
            
            // DON'T register any documents here - documents are created dynamically by DockFactory
            // when files are opened. The Editor component only provides the View/ViewModel types.
            // DockFactory will create DocumentViewModel instances as needed.
        }
        
        /// <summary>
        /// Factory method for DockFactory to create DocumentViewModel instances with proper dependencies
        /// </summary>
        public static DocumentViewModel CreateDocument(string id, string title)
        {
            if (_textMateService == null)
                _textMateService = new TextMateService();
                
            return new DocumentViewModel(id, title, _textMateService);
        }
    }
}