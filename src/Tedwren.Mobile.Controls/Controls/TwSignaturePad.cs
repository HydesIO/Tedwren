using Microsoft.Maui.Graphics.Platform;

namespace Tedwren.Mobile.Controls.Controls;

/// <summary>
/// A finger/stylus signature pad (M6 forms Signature field). Records strokes on a <see cref="GraphicsView"/> and
/// exports them as a PNG data URL — the exact shape the forms engine stores on the answer value (the console PDF
/// renderer embeds it). Built from raw MAUI graphics + the Tedwren tokens.
/// </summary>
public sealed class TwSignaturePad : GraphicsView, IDrawable
{
    private readonly List<List<PointF>> _strokes = new();
    private List<PointF>? _current;

    /// <summary>Builds the pad with a bordered surface and wires the drawing interactions.</summary>
    public TwSignaturePad()
    {
        Drawable = this;
        HeightRequest = 160;
        StartInteraction += OnStart;
        DragInteraction += OnDrag;
        // Announce the pad to assistive technology; drawing a signature itself needs sighted touch input (M8 a11y).
        SemanticProperties.SetDescription(this, "Signature pad. Draw your signature with your finger or a stylus.");
    }

    /// <summary>Whether the operative has drawn anything.</summary>
    public bool HasSignature => _strokes.Any(s => s.Count > 1);

    /// <summary>Clears the pad.</summary>
    public void Clear()
    {
        _strokes.Clear();
        _current = null;
        Invalidate();
    }

    private void OnStart(object? sender, TouchEventArgs e)
    {
        _current = new List<PointF>(e.Touches);
        _strokes.Add(_current);
        Invalidate();
    }

    private void OnDrag(object? sender, TouchEventArgs e)
    {
        if (_current is null)
        {
            return;
        }

        _current.AddRange(e.Touches);
        Invalidate();
    }

    /// <inheritdoc />
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Colors.White;
        canvas.FillRectangle(dirtyRect);
        DrawStrokes(canvas);
    }

    private void DrawStrokes(ICanvas canvas)
    {
        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize = 2;
        canvas.StrokeLineCap = LineCap.Round;
        foreach (var stroke in _strokes)
        {
            for (var i = 1; i < stroke.Count; i++)
            {
                canvas.DrawLine(stroke[i - 1], stroke[i]);
            }
        }
    }

    /// <summary>Renders the signature to a PNG <c>data:</c> URL, or null when the pad is empty / export is unavailable.</summary>
    public string? ToPngDataUrl()
    {
        if (!HasSignature)
        {
            return null;
        }

        try
        {
            var width = (int)Math.Max(Width, 1);
            var height = (int)Math.Max(Height, 1);
            using var context = new PlatformBitmapExportService().CreateContext(width, height);
            context.Canvas.FillColor = Colors.White;
            context.Canvas.FillRectangle(0, 0, width, height);
            DrawStrokes(context.Canvas);
            using var stream = new MemoryStream();
            context.Image.Save(stream);
            return "data:image/png;base64," + Convert.ToBase64String(stream.ToArray());
        }
        catch
        {
            return null; // export not available on this platform surface — treated as no signature.
        }
    }
}
