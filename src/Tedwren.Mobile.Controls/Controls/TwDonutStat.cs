using Tedwren.Mobile.Controls.Theme;

namespace Tedwren.Mobile.Controls.Controls;

/// <summary>
/// A compliance donut (M8): draws the workforce-compliance split (compliant / at-risk / non-compliant / pending) as
/// a ring with a centred label, superseding the M7 segmented bar on the dashboards. Uses <see cref="GraphicsView"/>
/// + <see cref="IDrawable"/> like <c>TwSignaturePad</c>, picks theme-aware colours from the design tokens, and
/// exposes a screen-reader description of the split (the ring itself is a visual summary).
/// </summary>
public class TwDonutStat : GraphicsView, IDrawable
{
    /// <summary>Count of compliant operatives.</summary>
    public static readonly BindableProperty CompliantProperty = Bindable(nameof(Compliant));

    /// <summary>Count of at-risk operatives.</summary>
    public static readonly BindableProperty AtRiskProperty = Bindable(nameof(AtRisk));

    /// <summary>Count of non-compliant operatives.</summary>
    public static readonly BindableProperty NonCompliantProperty = Bindable(nameof(NonCompliant));

    /// <summary>Count of pending (not-yet-assessed) operatives.</summary>
    public static readonly BindableProperty PendingProperty = Bindable(nameof(Pending));

    /// <summary>The label drawn in the centre of the ring (e.g. "91%").</summary>
    public static readonly BindableProperty CentreTextProperty =
        BindableProperty.Create(nameof(CentreText), typeof(string), typeof(TwDonutStat), string.Empty,
            propertyChanged: OnChanged);

    /// <summary>Count of compliant operatives.</summary>
    public int Compliant { get => (int)GetValue(CompliantProperty); set => SetValue(CompliantProperty, value); }

    /// <summary>Count of at-risk operatives.</summary>
    public int AtRisk { get => (int)GetValue(AtRiskProperty); set => SetValue(AtRiskProperty, value); }

    /// <summary>Count of non-compliant operatives.</summary>
    public int NonCompliant { get => (int)GetValue(NonCompliantProperty); set => SetValue(NonCompliantProperty, value); }

    /// <summary>Count of pending operatives.</summary>
    public int Pending { get => (int)GetValue(PendingProperty); set => SetValue(PendingProperty, value); }

    /// <summary>The centre label.</summary>
    public string CentreText { get => (string)GetValue(CentreTextProperty); set => SetValue(CentreTextProperty, value); }

    /// <summary>Wires the drawable and a default size.</summary>
    public TwDonutStat()
    {
        Drawable = this;
        HeightRequest = 140;
        WidthRequest = 140;
    }

    /// <summary>Draws the ring segments and the centre label.</summary>
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var total = Compliant + AtRisk + NonCompliant + Pending;
        var inset = 12f;
        var rect = new RectF(dirtyRect.X + inset, dirtyRect.Y + inset, dirtyRect.Width - 2 * inset, dirtyRect.Height - 2 * inset);
        canvas.StrokeSize = 18;
        canvas.StrokeLineCap = LineCap.Butt;

        if (total <= 0)
        {
            canvas.StrokeColor = Themed(TwPalette.BorderLight, TwPalette.BorderDark);
            canvas.DrawArc(rect.X, rect.Y, rect.Width, rect.Height, 0, 360, true, false);
        }
        else
        {
            var start = 90f; // 12 o'clock
            start = DrawSegment(canvas, rect, start, Compliant, total, Themed(TwPalette.SuccessLight, TwPalette.SuccessDark));
            start = DrawSegment(canvas, rect, start, AtRisk, total, Themed(TwPalette.WarningLight, TwPalette.WarningDark));
            start = DrawSegment(canvas, rect, start, NonCompliant, total, Themed(TwPalette.DangerLight, TwPalette.DangerDark));
            DrawSegment(canvas, rect, start, Pending, total, Themed(TwPalette.BorderLight, TwPalette.BorderDark));
        }

        if (!string.IsNullOrEmpty(CentreText))
        {
            canvas.FontColor = Themed(TwPalette.TextPrimaryLight, TwPalette.TextPrimaryDark);
            canvas.FontSize = 22;
            canvas.DrawString(CentreText, dirtyRect, HorizontalAlignment.Center, VerticalAlignment.Center);
        }
    }

    private static float DrawSegment(ICanvas canvas, RectF rect, float start, int value, int total, Color colour)
    {
        if (value <= 0)
        {
            return start;
        }

        var sweep = 360f * value / total;
        canvas.StrokeColor = colour;
        // MAUI Graphics arc angles are counter-clockwise from 3 o'clock; sweep clockwise for a natural donut.
        canvas.DrawArc(rect.X, rect.Y, rect.Width, rect.Height, start, start - sweep, true, false);
        return start - sweep;
    }

    private static Color Themed(Color light, Color dark) =>
        Application.Current?.RequestedTheme == AppTheme.Dark ? dark : light;

    private static BindableProperty Bindable(string name) =>
        BindableProperty.Create(name, typeof(int), typeof(TwDonutStat), 0, propertyChanged: OnChanged);

    private static void OnChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var donut = (TwDonutStat)bindable;
        donut.Invalidate();
        var total = donut.Compliant + donut.AtRisk + donut.NonCompliant + donut.Pending;
        SemanticProperties.SetDescription(donut,
            $"Workforce compliance. Compliant {donut.Compliant}, at risk {donut.AtRisk}, non-compliant {donut.NonCompliant}, pending {donut.Pending}, of {total}.");
    }
}
