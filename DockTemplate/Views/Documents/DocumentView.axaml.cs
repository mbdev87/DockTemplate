using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using DockTemplate.ViewModels.Documents;
using ReactiveUI;

namespace DockTemplate.Views.Documents;

public partial class DocumentView : UserControl, IDisposable
{
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
        if (DataContext is DocumentViewModel viewModel)
        {
            viewModel.SetupTextMateForEditor(TextEditor);
            SetupLineHighlighting(viewModel);
            SubscribeToThemeChanges(viewModel);
            
            _isEditorReady = true;
            AnnounceReady(viewModel, "OnLoaded");
            
            if (_pendingHighlightLine.HasValue)
            {
                HighlightLine(_pendingHighlightLine.Value);
                _pendingHighlightLine = null;
            }
        }
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
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
            var readyMessage = new DockTemplate.Messages.EditorReadyMessage(
                viewModel.FilePath, 
                viewModel.Title
            );
            MessageBus.Current.SendMessage(readyMessage);
        }
    }

    private void SubscribeToThemeChanges(DocumentViewModel viewModel)
    {
        _disposables.Clear();
        
        MessageBus.Current.Listen<DockTemplate.Messages.ThemeChangedMessage>()
            .Subscribe(message =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    SetupLineHighlighting(viewModel);
                }, DispatcherPriority.Background);
            })
            .DisposeWith(_disposables);

        MessageBus.Current.Listen<DockTemplate.Messages.ErrorNavigationMessage>()
            .Subscribe(message =>
            {
                if (viewModel.FilePath != null && 
                    string.Equals(viewModel.FilePath, message.FilePath, StringComparison.OrdinalIgnoreCase))
                {
                    if (_isEditorReady)
                    {
                        HighlightLine(message.LineNumber);
                    }
                    else
                    {
                        _pendingHighlightLine = message.LineNumber;
                    }
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

        if (_lineHighlightRenderer != null)
        {
            TextEditor.TextArea.TextView.BackgroundRenderers.Remove(_lineHighlightRenderer);
            _lineHighlightRenderer = null;
        }

        viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;

        Dispatcher.UIThread.Post(() =>
        {
            _lineHighlightRenderer = new LineHighlightRenderer();
            TextEditor.TextArea.TextView.BackgroundRenderers.Add(_lineHighlightRenderer);
        }, DispatcherPriority.Background);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
    }

    private void HighlightLine(int lineNumber)
    {
        if (_lineHighlightRenderer == null)
        {
            _lineHighlightRenderer = new LineHighlightRenderer();
            TextEditor.TextArea.TextView.BackgroundRenderers.Add(_lineHighlightRenderer);
        }

        _lineHighlightRenderer.HighlightedLine = null;
        _lineHighlightRenderer.HighlightedLine = lineNumber;
        
        TextEditor.ScrollToLine(lineNumber);
        TextEditor.Focus();
        
        TextEditor.TextArea.TextView.BackgroundRenderers.Remove(_lineHighlightRenderer);
        TextEditor.TextArea.TextView.BackgroundRenderers.Add(_lineHighlightRenderer);
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