using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using DockComponent.Editor.ViewModels.Documents;
using DockComponent.Editor.Transport;
using DockComponent.Base;
using JetBrains.Annotations;
using ReactiveUI;
// ReSharper disable PartialTypeWithSinglePart

namespace DockComponent.Editor.Views.Documents;

public partial class DocumentView : UserControl, IDisposable
{
    private TextEditor? _textEditor;
    private LineHighlightRenderer? _lineHighlightRenderer;
    private int? _pendingHighlightLine;
    private bool _isEditorReady = false;
    private readonly CompositeDisposable _disposables = new();

    public DocumentView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        GotFocus += OnGotFocus;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[DocumentView] OnLoaded called");
        
        // Get reference to the TextEditor from XAML
        _textEditor = this.FindControl<TextEditor>("TextEditor");
        
        if (DataContext is DocumentViewModel viewModel && _textEditor != null)
        {
            System.Diagnostics.Debug.WriteLine($"[DocumentView] OnLoaded for document: {viewModel.Title}");
            viewModel.SetupTextMateForEditor(_textEditor);
            SetupLineHighlighting(viewModel);
            SubscribeToThemeChanges(viewModel);
            
            _isEditorReady = true;
            System.Diagnostics.Debug.WriteLine($"[DocumentView] Editor ready for {viewModel.Title}");
            AnnounceReady(viewModel, "OnLoaded");
            
            if (_pendingHighlightLine.HasValue)
            {
                System.Diagnostics.Debug.WriteLine($"[DocumentView] Processing pending highlight line: {_pendingHighlightLine.Value}");
                HighlightLine(_pendingHighlightLine.Value);
                _pendingHighlightLine = null;
            }
        }
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[DocumentView] DataContext changed to: {(DataContext as DocumentViewModel)?.Title ?? "null"}");
        _isEditorReady = false;
        _pendingHighlightLine = null;
    }

    private void OnGotFocus(object? sender, Avalonia.Input.GotFocusEventArgs e)
    {
        if (DataContext is DocumentViewModel viewModel && _isEditorReady)
        {
            AnnounceReady(viewModel, "OnGotFocus");
        }
    }

    private void AnnounceReady(DocumentViewModel viewModel, string trigger)
    {
        if (viewModel.FilePath != null)
        {
            // Send as ComponentMessage for cross-component communication
            var readyMessage = new EditorReadyMsg(
                viewModel.FilePath, 
                viewModel.Title
            );
            
            var componentMessage = new ComponentMessage(
                "Editor_EditorReady",
                System.Text.Json.JsonSerializer.Serialize(readyMessage)
            );
            
            MessageBus.Current.SendMessage(componentMessage);
            System.Diagnostics.Debug.WriteLine($"[DocumentView] Sent Editor_EditorReady for {viewModel.FilePath}");
        }
    }

    private void SubscribeToThemeChanges(DocumentViewModel viewModel)
    {
        _disposables.Clear();
        
        MessageBus.Current.Listen<ThemeChangedMsg>()
            .Subscribe(message =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    SetupLineHighlighting(viewModel);
                }, DispatcherPriority.Background);
            })
            .DisposeWith(_disposables);

        // Listen for ComponentMessage from ErrorList containing ErrorNavigationMsg
        MessageBus.Current.Listen<ComponentMessage>()
            .Where(msg => msg.Name == "ErrorList_ScrollToLine")
            .Subscribe(message =>
            {
                try
                {
                    var errorNavMsg = System.Text.Json.JsonSerializer.Deserialize<ErrorNavigationMsg>(message.Payload);
                    if (errorNavMsg != null && viewModel.FilePath != null && 
                        string.Equals(viewModel.FilePath, errorNavMsg.FilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        System.Diagnostics.Debug.WriteLine($"[DocumentView] Received scroll to line request for {System.IO.Path.GetFileName(errorNavMsg.FilePath)}:{errorNavMsg.LineNumber}");
                        if (_isEditorReady)
                        {
                            HighlightLine(errorNavMsg.LineNumber);
                        }
                        else
                        {
                            _pendingHighlightLine = errorNavMsg.LineNumber;
                            System.Diagnostics.Debug.WriteLine($"[DocumentView] Queued pending highlight for line {errorNavMsg.LineNumber}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DocumentView] Error parsing ErrorNavigationMsg: {ex.Message}");
                }
            })
            .DisposeWith(_disposables);
    }

    private void SetupLineHighlighting(DocumentViewModel viewModel)
    {
        if (DataContext is DocumentViewModel prevViewModel && prevViewModel != viewModel)
        {
            prevViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (_lineHighlightRenderer != null && _textEditor != null)
        {
            _textEditor.TextArea.TextView.BackgroundRenderers.Remove(_lineHighlightRenderer);
            _lineHighlightRenderer = null;
        }

        System.Diagnostics.Debug.WriteLine($"[DocumentView] Subscribing to PropertyChanged for {viewModel.Title}");
        viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;

        Dispatcher.UIThread.Post(() =>
        {
            if (_textEditor != null)
            {
                _lineHighlightRenderer = new LineHighlightRenderer();
                _textEditor.TextArea.TextView.BackgroundRenderers.Add(_lineHighlightRenderer);
            }
        }, DispatcherPriority.Background);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // No longer needed - using message bus for navigation
    }

    private void HighlightLine(int lineNumber)
    {
        if (_textEditor == null) return;
        
        if (_lineHighlightRenderer == null)
        {
            _lineHighlightRenderer = new LineHighlightRenderer();
            _textEditor.TextArea.TextView.BackgroundRenderers.Add(_lineHighlightRenderer);
        }

        System.Diagnostics.Debug.WriteLine($"[DocumentView] HighlightLine called for line {lineNumber}");

        // Post to UI thread with slight delay to ensure editor is fully rendered
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                if (_textEditor == null) return;
                
                // Small delay to ensure UI layout is complete
                await System.Threading.Tasks.Task.Delay(50);

                // Validate line number and set caret position
                if (lineNumber > 0 && lineNumber <= _textEditor.Document.LineCount)
                {
                    var line = _textEditor.Document.GetLineByNumber(lineNumber);
                    _textEditor.CaretOffset = line.Offset;
                }

                _lineHighlightRenderer.HighlightedLine = null;
                _lineHighlightRenderer.HighlightedLine = lineNumber;
                
                _textEditor.ScrollToLine(lineNumber);
                _textEditor.Focus();
                
                // Force aggressive visual refresh by temporarily removing/re-adding renderer
                _textEditor.TextArea.TextView.BackgroundRenderers.Remove(_lineHighlightRenderer);
                _textEditor.TextArea.TextView.BackgroundRenderers.Add(_lineHighlightRenderer);
                
                // Force complete editor refresh
                _textEditor.TextArea.TextView.InvalidateVisual();
                _textEditor.TextArea.TextView.InvalidateMeasure();
                _textEditor.TextArea.TextView.InvalidateArrange();
                
                // Force focus cycle to trigger visual updates
                _textEditor.Focus(Avalonia.Input.NavigationMethod.Unspecified);
                await System.Threading.Tasks.Task.Delay(1);
                _textEditor.Focus();
                
                System.Diagnostics.Debug.WriteLine($"[DocumentView] Line {lineNumber} highlighted and scrolled to");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DocumentView] Error highlighting line {lineNumber}: {ex.Message}");
            }
        }, DispatcherPriority.Background);
    }

    public void Dispose()
    {
        _disposables?.Dispose();
    }
}

public class LineHighlightRenderer : IBackgroundRenderer
{
    public int? HighlightedLine { get; set; }
    
    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (!HighlightedLine.HasValue || textView.Document == null)
            return;

        try
        {
            var lineNumber = HighlightedLine.Value;
            if (lineNumber <= 0 || lineNumber > textView.Document.LineCount)
                return;

            var line = textView.Document.GetLineByNumber(lineNumber);
            
            foreach (var visualLine in textView.VisualLines)
            {
                if (visualLine.FirstDocumentLine.LineNumber <= lineNumber && 
                    visualLine.LastDocumentLine.LineNumber >= lineNumber)
                {
                    var lineTop = visualLine.VisualTop - textView.ScrollOffset.Y;
                    var lineHeight = visualLine.Height;
                    
                    if (lineTop + lineHeight >= 0 && lineTop <= textView.Bounds.Height)
                    {
                        var rect = new Rect(
                            0, 
                            lineTop,
                            Math.Max(textView.Bounds.Width, 2000),
                            lineHeight);

                        var brush = new SolidColorBrush(Color.FromArgb(120, 255, 255, 0));
                        drawingContext.DrawRectangle(brush, null, rect);
                    }
                }
            }
        }
        catch
        {
            // Silently handle any drawing errors
        }
    }
}