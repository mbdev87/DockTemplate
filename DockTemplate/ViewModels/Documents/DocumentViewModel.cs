using System;
using System.IO;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Styling;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
using Dock.Model.Mvvm.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TextMateSharp.Grammars;
using DockTemplate.Messages;
using DockTemplate.Services;
using NLog;

namespace DockTemplate.ViewModels.Documents;

public class DocumentViewModel : Document, IDisposable
{
    [Reactive] public TextDocument Document { get; set; } = new();
    [Reactive] public string CurrentLanguage { get; set; } = "plaintext";
    [Reactive] public bool HasUnsavedChanges { get; set; }
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly CompositeDisposable _disposables = new();
    private readonly TextMateService _textMateService;
    private TextMateService.TextMateContext? _textMateContext;

    public DocumentViewModel(TextMateService textMateService)
    {
        _textMateService = textMateService;
        Document.TextChanged += (_, _) => HasUnsavedChanges = true;
        
        // Subscribe to theme changes
        MessageBus.Current.Listen<ThemeChangedMessage>()
            .Subscribe(message => 
            {
                Logger.Info($"[{Title}] Received theme change message: {message.NewTheme}");
                _textMateService.UpdateTheme(message.NewTheme);
            })
            .DisposeWith(_disposables);
    }

    public DocumentViewModel(string id, string title, TextMateService textMateService) : this(textMateService)
    {
        Id = id;
        Title = title;
        CurrentLanguage = DetectLanguageFromTitle(title);
    }

    public void SetupTextMateForEditor(TextEditor textEditor)
    {
        try
        {
            // Use the document ID as cache key - this allows caching across editor switches
            var documentId = !string.IsNullOrEmpty(Title) ? Title : Id ?? "unknown";
            
            Logger.Info($"[{Title}] Setting up TextMate using service for document: {documentId}");
            
            // Get or create cached TextMate context
            _textMateContext = _textMateService.GetOrCreateContext(documentId, CurrentLanguage, textEditor);
            
            Logger.Info($"[{Title}] TextMate context ready for {documentId}");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Error setting up TextMate for {Title}: {ex.Message}");
        }
    }


    public void SetContent(string content)
    {
        Document.Text = content;
        HasUnsavedChanges = false;
    }


    private string DetectLanguageFromTitle(string title)
    {
        var extension = Path.GetExtension(title).ToLowerInvariant();
        return extension switch
        {
            ".cs" => "csharp",
            ".txt" => "plaintext",
            ".md" => "markdown",
            ".json" => "json",
            ".xml" => "xml",
            ".js" => "javascript",
            ".ts" => "typescript",
            ".py" => "python",
            ".ps1" => "powershell",
            ".sh" => "bash",
            ".bat" => "batch",
            _ => "plaintext"
        };
    }

    private string GetFileExtensionForLanguage(string language)
    {
        return language.ToLower() switch
        {
            "csharp" or "c#" => ".cs",
            "markdown" => ".md",
            "plaintext" => ".txt",
            "json" => ".json",
            "xml" => ".xml",
            "javascript" => ".js",
            "typescript" => ".ts",
            "python" => ".py",
            "powershell" => ".ps1",
            "bash" => ".sh",
            "batch" => ".bat",
            _ => ".txt"
        };
    }

    public void Dispose()
    {
        // Notify service that this document is being disposed
        if (_textMateContext != null && !string.IsNullOrEmpty(Title))
        {
            var documentId = !string.IsNullOrEmpty(Title) ? Title : Id ?? "unknown";
            _textMateService.RemoveContext(documentId);
        }
        
        _disposables?.Dispose();
    }
}