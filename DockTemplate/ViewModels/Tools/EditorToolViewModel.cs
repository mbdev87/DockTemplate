using System;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TextMateSharp.Grammars;
using DockTemplate.Messages;
using NLog;

namespace DockTemplate.ViewModels.Tools;

public class EditorToolViewModel : ViewModelBase, IDisposable
{
    [Reactive] public TextDocument Document { get; set; } = new();
    [Reactive] public TextMate.Installation? TextMateInstallation { get; set; }
    [Reactive] public string CurrentFileName { get; set; } = "untitled.txt";
    [Reactive] public string CurrentLanguage { get; set; } = "plaintext";
    [Reactive] public bool HasUnsavedChanges { get; set; }
    [Reactive] public string StatusText { get; set; } = "Ready";
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public ReactiveCommand<Unit, Unit> OpenFileCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveFileCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveAsCommand { get; }
    public ReactiveCommand<Unit, Unit> NewFileCommand { get; }

    private readonly CompositeDisposable _disposables = new();
    private string _currentFilePath = string.Empty;
    private TextEditor? _textEditor;
    private DockTemplate.Views.Documents.LineHighlightRenderer? _lineHighlightRenderer;

    public EditorToolViewModel()
    {
        OpenFileCommand = ReactiveCommand.CreateFromTask(OpenFileAsync);
        SaveFileCommand = ReactiveCommand.CreateFromTask(SaveFileAsync, 
            this.WhenAnyValue(x => x.HasUnsavedChanges));
        SaveAsCommand = ReactiveCommand.CreateFromTask(SaveAsFileAsync);
        NewFileCommand = ReactiveCommand.Create(NewFile);

        Document.TextChanged += (_, _) =>
        {
            if (!string.IsNullOrEmpty(_currentFilePath))
            {
                HasUnsavedChanges = true;
                StatusText = "Modified";
            }
        };

        // Subscribe to theme changes
        MessageBus.Current.Listen<ThemeChangedMessage>()
            .Subscribe(message => ApplySyntaxHighlighting())
            .DisposeWith(_disposables);

        // Subscribe to error navigation messages
        MessageBus.Current.Listen<DockTemplate.Messages.ErrorNavigationMessage>()
            .Subscribe(message => HandleErrorNavigation(message))
            .DisposeWith(_disposables);

        ReinstallTextMate();
    }

    public void SetupTextMateForEditor(TextEditor textEditor)
    {
        try
        {
            _textEditor = textEditor;
            ReinstallTextMate();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Error setting up TextMate: {ex.Message}");
            StatusText = "Syntax highlighting unavailable";
        }
    }

    private void ReinstallTextMate()
    {
        if (_textEditor == null) return;

        try
        {
            // Get current theme - check both ActualThemeVariant and RequestedThemeVariant
            var currentApp = Application.Current;
            var actualTheme = currentApp?.ActualThemeVariant;
            var requestedTheme = currentApp?.RequestedThemeVariant;
            
            // Determine if we're in dark mode
            var isDark = (actualTheme == ThemeVariant.Dark) || 
                        (actualTheme == null && requestedTheme == ThemeVariant.Dark);
            
            var themeName = isDark ? ThemeName.DarkPlus : ThemeName.LightPlus;
            
            Logger.Info($"[Editor] Reinstalling TextMate: ActualTheme={actualTheme}, RequestedTheme={requestedTheme}, Using={themeName}");
            
            // Dispose old installation if it exists
            var old = TextMateInstallation;
            
            // Create new registry options with current theme
            var registryOptions = new RegistryOptions(themeName);
            
            // Reinstall TextMate completely
            TextMateInstallation = _textEditor.InstallTextMate(registryOptions);
            
            // Apply grammar for current language
            var fileExtension = GetFileExtensionForLanguage(CurrentLanguage);
            var language = registryOptions.GetLanguageByExtension(fileExtension);
            
            if (language != null)
            {
                TextMateInstallation.SetGrammar(registryOptions.GetScopeByLanguageId(language.Id));
                Logger.Info($"[Editor] Applied grammar for {CurrentLanguage}");
            }
            
            // Force refresh the editor content to trigger highlighting
            if (!string.IsNullOrEmpty(Document.Text))
            {
                var currentText = Document.Text;
                var cursorPosition = _textEditor.CaretOffset;
                Document.Text = string.Empty;
                Document.Text = currentText;
                _textEditor.CaretOffset = cursorPosition;
                Logger.Info($"[Editor] Forced content refresh");
            }
            old?.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Error reinstalling TextMate: {ex.Message}");
            StatusText = "Syntax highlighting unavailable";
        }
    }

    private void ApplySyntaxHighlighting()
    {
        // Use the more aggressive reinstall approach
        ReinstallTextMate();
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

    private string DetectLanguageFromExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".cs" => "csharp",
            ".md" => "markdown", 
            ".txt" => "plaintext",
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

    private async Task OpenFileAsync()
    {
        try
        {
            // In a real implementation, use OpenFileDialog
            // For now, simulate with a hardcoded file for demo
            var filePath = @"C:\temp\sample.cs"; // This would come from file dialog
            
            if (File.Exists(filePath))
            {
                var content = await File.ReadAllTextAsync(filePath);
                Document.Text = content;
                CurrentFileName = Path.GetFileName(filePath);
                CurrentLanguage = DetectLanguageFromExtension(CurrentFileName);
                _currentFilePath = filePath;
                HasUnsavedChanges = false;
                StatusText = $"Opened: {CurrentFileName}";
                ApplySyntaxHighlighting();
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error opening file: {ex.Message}";
        }
    }

    private async Task SaveFileAsync()
    {
        if (string.IsNullOrEmpty(_currentFilePath))
        {
            await SaveAsFileAsync();
            return;
        }

        try
        {
            await File.WriteAllTextAsync(_currentFilePath, Document.Text);
            HasUnsavedChanges = false;
            StatusText = $"Saved: {CurrentFileName}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error saving file: {ex.Message}";
        }
    }

    private async Task SaveAsFileAsync()
    {
        try
        {
            // In a real implementation, use SaveFileDialog
            // For now, simulate with a temp file
            var fileName = CurrentFileName == "untitled.txt" ? "new_file.txt" : CurrentFileName;
            var filePath = Path.Combine(Path.GetTempPath(), fileName);
            
            await File.WriteAllTextAsync(filePath, Document.Text);
            CurrentFileName = Path.GetFileName(filePath);
            _currentFilePath = filePath;
            HasUnsavedChanges = false;
            StatusText = $"Saved as: {CurrentFileName}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error saving file: {ex.Message}";
        }
    }

    private void NewFile()
    {
        Document.Text = string.Empty;
        CurrentFileName = "untitled.txt";
        CurrentLanguage = "plaintext";
        _currentFilePath = string.Empty;
        HasUnsavedChanges = false;
        StatusText = "New file";
        ApplySyntaxHighlighting();
    }

    public async Task OpenFileWithPath(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var content = await File.ReadAllTextAsync(filePath);
                Document.Text = content;
                CurrentFileName = Path.GetFileName(filePath);
                CurrentLanguage = DetectLanguageFromExtension(CurrentFileName);
                _currentFilePath = filePath;
                HasUnsavedChanges = false;
                StatusText = $"Opened: {CurrentFileName}";
                ApplySyntaxHighlighting();
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error opening {filePath}: {ex.Message}";
        }
    }

    private void HandleErrorNavigation(DockTemplate.Messages.ErrorNavigationMessage message)
    {
        // Only process if this editor is for the target file
        if (!string.IsNullOrEmpty(_currentFilePath) && 
            string.Equals(_currentFilePath, message.FilePath, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Info($"[EditorToolViewModel] Received error navigation for MY file: {message}");
            NavigateToLine(message.LineNumber);
        }
        else
        {
            Logger.Info($"[EditorToolViewModel] Ignoring error navigation for different file. My file: {_currentFilePath}, Target: {message.FilePath}");
        }
    }

    private void NavigateToLine(int lineNumber)
    {
        if (_textEditor == null)
        {
            Logger.Warn($"[EditorToolViewModel] Cannot navigate - no text editor available");
            return;
        }

        try
        {
            Logger.Info($"[EditorToolViewModel] Navigating to line {lineNumber}");

            // Ensure line highlighting renderer is set up
            SetupLineHighlighting();

            // Clear old highlight
            if (_lineHighlightRenderer != null)
            {
                var oldLine = _lineHighlightRenderer.HighlightedLine;
                _lineHighlightRenderer.HighlightedLine = null;
                Logger.Info($"[EditorToolViewModel] Cleared old highlight (was line {oldLine})");
            }

            // Validate line number
            if (lineNumber <= 0 || lineNumber > _textEditor.Document.LineCount)
            {
                Logger.Warn($"[EditorToolViewModel] Invalid line number: {lineNumber} (max: {_textEditor.Document.LineCount})");
                return;
            }

            // Get the line and set caret
            var line = _textEditor.Document.GetLineByNumber(lineNumber);
            _textEditor.CaretOffset = line.Offset;
            
            // Scroll to line and focus
            _textEditor.ScrollToLine(lineNumber);
            _textEditor.Focus();

            // Set new highlight and force redraw
            if (_lineHighlightRenderer != null)
            {
                _lineHighlightRenderer.HighlightedLine = lineNumber;
                Logger.Info($"[EditorToolViewModel] Set highlight to line {lineNumber}");

                // Force complete redraw by removing and re-adding renderer
                _textEditor.TextArea.TextView.BackgroundRenderers.Remove(_lineHighlightRenderer);
                _textEditor.TextArea.TextView.BackgroundRenderers.Add(_lineHighlightRenderer);
                
                // Force visual refresh
                _textEditor.TextArea.TextView.InvalidateVisual();
                
                Logger.Info($"[EditorToolViewModel] Forced renderer refresh for line {lineNumber}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"[EditorToolViewModel] Error navigating to line {lineNumber}");
        }
    }

    private void SetupLineHighlighting()
    {
        if (_textEditor == null) return;

        // Remove existing renderer if any
        if (_lineHighlightRenderer != null)
        {
            _textEditor.TextArea.TextView.BackgroundRenderers.Remove(_lineHighlightRenderer);
        }

        // Create and add new renderer
        _lineHighlightRenderer = new DockTemplate.Views.Documents.LineHighlightRenderer();
        _textEditor.TextArea.TextView.BackgroundRenderers.Add(_lineHighlightRenderer);
        
        Logger.Info($"[EditorToolViewModel] Set up line highlighting renderer");
    }

    public void Dispose()
    {
        TextMateInstallation?.Dispose();
        _disposables?.Dispose();
    }
}