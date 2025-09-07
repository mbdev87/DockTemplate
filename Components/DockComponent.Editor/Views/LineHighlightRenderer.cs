using System;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;

namespace DockComponent.Editor.Views;

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