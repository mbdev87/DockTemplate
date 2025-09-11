using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.TextMate;
using NLog;
using TextMateSharp.Grammars;

namespace DockComponent.Editor.Services;

public class TextMateService
{
    private readonly ConcurrentDictionary<string, TextMateContext> _contexts = new();
    private ThemeVariant _currentTheme = ThemeVariant.Default;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public class TextMateContext
    {
        public TextMate.Installation Installation { get; set; } = null!;
        public RegistryOptions Registry { get; set; } = null!;
        public string Language { get; set; } = string.Empty;
        public ThemeName CurrentTheme { get; set; }
        public TextEditor? AttachedEditor { get; set; }
        public bool IsDisposed { get; set; }
        public bool NeedsRefresh { get; set; }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                Installation?.Dispose();
                Registry = null!;
                IsDisposed = true;
            }
        }
    }

    public TextMateContext GetOrCreateContext(string documentId, string language, TextEditor textEditor)
    {
        var currentThemeName = GetCurrentThemeName();
        
        // Try to get existing context
        if (_contexts.TryGetValue(documentId, out var existingContext) && 
            !existingContext.IsDisposed &&
            existingContext.Language == language && 
            existingContext.CurrentTheme == currentThemeName)
        {
            Logger.Info($"[TextMateService] Reusing cached context for {documentId}");
            
            // Reattach to new editor if different
            if (existingContext.AttachedEditor != textEditor)
            {
                AttachToEditor(existingContext, textEditor);
            }
            
            // Check if context needs refresh due to theme change
            if (existingContext.NeedsRefresh)
            {
                Logger.Info($"[TextMateService] Context {documentId} needs refresh - applying now to current editor");
                existingContext.NeedsRefresh = false;
                
                // CRITICAL: Ensure we refresh the CURRENT editor, not a cached one
                var currentEditor = existingContext.AttachedEditor;
                if (currentEditor == textEditor)
                {
                    Logger.Info($"[TextMateService] Refreshing content on current editor");
                    // Refresh the content now that this tab is active
                    Dispatcher.UIThread.Post(async () =>
                    {
                        await System.Threading.Tasks.Task.Delay(10); // Minimal delay for tab switch
                        await RefreshEditorContent(existingContext);
                    }, DispatcherPriority.Background);
                }
                else
                {
                    Logger.Warn($"[TextMateService] Editor mismatch detected - context attached to different editor");
                }
            }
            
            return existingContext;
        }

        Logger.Info($"[TextMateService] Creating new context for {documentId} ({language}, {currentThemeName})");

        // Dispose old context if it exists
        if (existingContext != null)
        {
            existingContext.Dispose();
        }

        // Create new context
        var newContext = CreateContext(language, currentThemeName, textEditor);
        _contexts[documentId] = newContext;
        
        return newContext;
    }

    private TextMateContext CreateContext(string language, ThemeName themeName, TextEditor textEditor)
    {
        // Create dedicated registry for this context to avoid global interference
        var registryOptions = new RegistryOptions(themeName);
        var installation = textEditor.InstallTextMate(registryOptions);

        // Apply grammar for the specified language
        var fileExtension = GetFileExtensionForLanguage(language);
        var languageInfo = registryOptions.GetLanguageByExtension(fileExtension);
        
        if (languageInfo != null)
        {
            installation.SetGrammar(registryOptions.GetScopeByLanguageId(languageInfo.Id));
        }

        var context = new TextMateContext
        {
            Installation = installation,
            Registry = registryOptions, // Store the registry reference
            Language = language,
            CurrentTheme = themeName,
            AttachedEditor = textEditor,
            IsDisposed = false
        };

        return context;
    }

    private void AttachToEditor(TextMateContext context, TextEditor textEditor)
    {
        // TextMate installations are editor-specific, so we need to recreate 
        // the installation entirely for the new editor
        if (context.AttachedEditor != textEditor)
        {
            Logger.Info($"[TextMateService] Reattaching context to different editor - creating new installation");
            Logger.Info($"[TextMateService] Old editor: {context.AttachedEditor?.GetHashCode()}, New editor: {textEditor.GetHashCode()}");
            
            var oldInstallation = context.Installation;
            
            // Create a fresh registry instance to avoid global pollution
            var freshRegistry = new RegistryOptions(context.CurrentTheme);
            context.Registry = freshRegistry;
            
            // Install TextMate on the new editor with fresh registry
            context.Installation = textEditor.InstallTextMate(freshRegistry);
            context.AttachedEditor = textEditor;
            
            // Apply grammar using the fresh registry
            var fileExtension = GetFileExtensionForLanguage(context.Language);
            var languageInfo = freshRegistry.GetLanguageByExtension(fileExtension);
            
            if (languageInfo != null)
            {
                context.Installation.SetGrammar(freshRegistry.GetScopeByLanguageId(languageInfo.Id));
            }
            
            // Dispose old installation after successful setup
            oldInstallation?.Dispose();
        }
    }

    public void UpdateTheme(ThemeVariant newTheme)
    {
        if (_currentTheme == newTheme) return;
        
        _currentTheme = newTheme;
        // Convert the passed theme directly instead of reading from Application.Current
        var newThemeName = newTheme == ThemeVariant.Dark ? ThemeName.DarkPlus : ThemeName.LightPlus;
        
        Logger.Info($"[TextMateService] Updating all contexts to theme: {newTheme} -> {newThemeName}");

        // Update all existing contexts
        foreach (var kvp in _contexts)
        {
            var context = kvp.Value;
            if (context.IsDisposed || context.AttachedEditor == null) continue;

            try
            {
                // Recreate installation with new theme using fresh registry
                var oldInstallation = context.Installation;
                var freshRegistry = new RegistryOptions(newThemeName);
                
                context.Installation = context.AttachedEditor.InstallTextMate(freshRegistry);
                context.Registry = freshRegistry;
                context.CurrentTheme = newThemeName;
                
                // Reapply grammar with fresh registry
                var fileExtension = GetFileExtensionForLanguage(context.Language);
                var languageInfo = freshRegistry.GetLanguageByExtension(fileExtension);
                
                if (languageInfo != null)
                {
                    context.Installation.SetGrammar(freshRegistry.GetScopeByLanguageId(languageInfo.Id));
                }
                
                // Check if this context is currently active (attached editor is visible)
                if (context.AttachedEditor != null && context.AttachedEditor.IsVisible)
                {
                    // Immediately refresh active contexts
                    Logger.Info($"[TextMateService] Context {kvp.Key} is active - refreshing immediately");
                    // MULTI-PRIORITY APPROACH: Hit different points in the rendering pipeline
                    Logger.Info($"[TextMateService] Scheduling multi-priority refresh");
                    
                    // Attempt 1: Immediate with Render priority (highest)
                    Dispatcher.UIThread.Post(async () => 
                    {
                        Logger.Info($"[TextMateService] Render priority refresh");
                        await RefreshEditorContent(context);
                    }, DispatcherPriority.Render);
                    
                    // Attempt 2: Background priority as fallback
                    Dispatcher.UIThread.Post(async () => 
                    {
                        await System.Threading.Tasks.Task.Delay(1); // Tiny delay to ensure render priority ran
                        Logger.Info($"[TextMateService] Background priority refresh");  
                        await RefreshEditorContent(context);
                    }, DispatcherPriority.Background);
                }
                else
                {
                    // Mark inactive contexts for refresh when they become active
                    context.NeedsRefresh = true;
                    Logger.Info($"[TextMateService] Context {kvp.Key} is inactive - marked for refresh on activation");
                }
                
                // Dispose old installation
                oldInstallation?.Dispose();
                
                Logger.Info($"[TextMateService] Updated context {kvp.Key} to {newThemeName}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"[TextMateService] Error updating context {kvp.Key}: {ex.Message}");
            }
        }
    }

    private async Task RefreshEditorContent(TextMateContext context)
    {
        if (context.AttachedEditor == null) 
        {
            Logger.Warn($"[TextMateService] RefreshEditorContent: No attached editor for context");
            return;
        }

        try
        {
            var editor = context.AttachedEditor;
            var document = editor.Document;
            
            Logger.Info($"[TextMateService] RefreshEditorContent: Refreshing {context.Language} content, isVisible: {editor.IsVisible}");
            
            if (document != null && !string.IsNullOrEmpty(document.Text))
            {
                if (context.Language == "markdown")
                {
                    // Light refresh for Markdown
                    editor.InvalidateVisual();
                    Logger.Info($"[TextMateService] Light refresh applied for markdown");
                }
                else
                {
                    // Full refresh for other languages
                    var currentText = document.Text;
                    var cursorOffset = editor.CaretOffset;
                    
                    Logger.Info($"[TextMateService] Full refresh: clearing and restoring {currentText.Length} characters");
                    document.Text = string.Empty;
                    document.Text = currentText;
                    editor.CaretOffset = cursorOffset;
                    
                    // PROVEN TECHNIQUE: Use the same method that works for line highlighting
                    Logger.Info($"[TextMateService] Applying proven visual refresh technique...");
                    
                    // Force aggressive visual refresh by manipulating background renderers
                    if (editor.TextArea?.TextView != null)
                    {
                        var textView = editor.TextArea.TextView;
                        var backgroundRenderers = textView.BackgroundRenderers;
                        var rendererCount = backgroundRenderers.Count;
                        
                        // Temporarily remove and re-add all background renderers to force refresh
                        var renderersCopy = new List<AvaloniaEdit.Rendering.IBackgroundRenderer>();
                        for (int i = 0; i < backgroundRenderers.Count; i++)
                        {
                            renderersCopy.Add(backgroundRenderers[i]);
                        }
                        backgroundRenderers.Clear();
                        foreach (var renderer in renderersCopy)
                        {
                            backgroundRenderers.Add(renderer);
                        }
                        
                        Logger.Info($"[TextMateService] Refreshed {rendererCount} background renderers");
                    }
                    
                    // Apply the proven TextView invalidation sequence
                    if (editor.TextArea?.TextView != null)
                    {
                        editor.TextArea.TextView.InvalidateVisual();
                        editor.TextArea.TextView.InvalidateMeasure();
                        editor.TextArea.TextView.InvalidateArrange();
                    }
                    
                    // Apply the proven focus cycle technique with minimal delay
                    editor.Focus(Avalonia.Input.NavigationMethod.Unspecified);
                    // Ultra-minimal delay - just enough to trigger the pipeline
                    await System.Threading.Tasks.Task.Delay(1);
                    editor.Focus();
                    
                    Logger.Info($"[TextMateService] Proven visual refresh technique completed");
                }
            }
            else
            {
                Logger.Warn($"[TextMateService] RefreshEditorContent: Document is null or empty");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"[TextMateService] Error refreshing editor content: {ex.Message}");
        }
    }

    public void RemoveContext(string documentId)
    {
        if (_contexts.TryRemove(documentId, out var context))
        {
            Logger.Info($"[TextMateService] Removing context for {documentId}");
            context.Dispose();
        }
    }

    public void DisposeAll()
    {
        Logger.Info($"[TextMateService] Disposing all contexts ({_contexts.Count})");
        
        foreach (var context in _contexts.Values)
        {
            context.Dispose();
        }
        
        _contexts.Clear();
    }

    private ThemeName GetCurrentThemeName()
    {
        var app = Application.Current;
        var actualTheme = app?.ActualThemeVariant;
        var requestedTheme = app?.RequestedThemeVariant ?? _currentTheme;
        
        var isDark = (actualTheme == ThemeVariant.Dark) || 
                    (actualTheme == null && requestedTheme == ThemeVariant.Dark);
        
        return isDark ? ThemeName.DarkPlus : ThemeName.LightPlus;
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
}