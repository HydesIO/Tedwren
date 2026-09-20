using Microsoft.Maui.Controls.Shapes;
using Tedwren.Mobile.Controls.Theme;

namespace Tedwren.Mobile.Controls.Controls;

/// <summary>The semantic colour of a status pill.</summary>
public enum TwStatusKind
{
    /// <summary>Subtle neutral (unknown / informational).</summary>
    Neutral,

    /// <summary>Green (compliant / approved / covered).</summary>
    Success,

    /// <summary>Amber (at risk / awaiting review / last holder).</summary>
    Warning,

    /// <summary>Red (non-compliant / rejected / not covered).</summary>
    Danger,
}

/// <summary>
/// A small rounded status pill — a bold caption on a pale, theme-aware background in the colour of its
/// <see cref="Kind"/>. Reused across the manager dashboard, muster, operatives and forms review to render
/// compliance, risk, form-status and RAG states consistently. Colours use the design tokens (no literals).
/// </summary>
public class TwStatusPill : Border
{
    private readonly Label _label = new() { FontSize = 12, FontAttributes = FontAttributes.Bold };

    /// <summary>The pill's text.</summary>
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(TwStatusPill), string.Empty,
            propertyChanged: (bindable, _, newValue) => SemanticProperties.SetDescription((TwStatusPill)bindable, $"Status: {newValue}"));

    /// <summary>The pill's semantic colour.</summary>
    public static readonly BindableProperty KindProperty =
        BindableProperty.Create(nameof(Kind), typeof(TwStatusKind), typeof(TwStatusPill), TwStatusKind.Neutral,
            propertyChanged: (bindable, _, _) => ((TwStatusPill)bindable).ApplyColours());

    /// <summary>The pill's text.</summary>
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>The pill's semantic colour.</summary>
    public TwStatusKind Kind { get => (TwStatusKind)GetValue(KindProperty); set => SetValue(KindProperty, value); }

    /// <summary>Builds the pill chrome and binds its text.</summary>
    public TwStatusPill()
    {
        Padding = new Thickness(10, 4);
        StrokeThickness = 0;
        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(999) };
        HorizontalOptions = LayoutOptions.Start;
        _label.SetBinding(Label.TextProperty, new Binding(nameof(Text), source: this));
        Content = _label;
        ApplyColours();
    }

    /// <summary>Applies the pale background + solid text colours for the current <see cref="Kind"/> in both themes.</summary>
    private void ApplyColours()
    {
        var (paleLight, paleDark, solidLight, solidDark) = Kind switch
        {
            TwStatusKind.Success => (TwPalette.SuccessPaleLight, TwPalette.SuccessPaleDark, TwPalette.SuccessLight, TwPalette.SuccessDark),
            TwStatusKind.Warning => (TwPalette.WarningPaleLight, TwPalette.WarningPaleDark, TwPalette.WarningLight, TwPalette.WarningDark),
            TwStatusKind.Danger => (TwPalette.DangerPaleLight, TwPalette.DangerPaleDark, TwPalette.DangerLight, TwPalette.DangerDark),
            _ => (TwPalette.BorderLight, TwPalette.BorderDark, TwPalette.TextSecondaryLight, TwPalette.TextSecondaryDark),
        };

        this.SetAppThemeColor(BackgroundColorProperty, paleLight, paleDark);
        _label.SetAppThemeColor(Label.TextColorProperty, solidLight, solidDark);
    }
}
