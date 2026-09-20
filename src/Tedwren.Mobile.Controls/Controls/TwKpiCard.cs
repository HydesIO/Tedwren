using Tedwren.Mobile.Controls.Theme;

namespace Tedwren.Mobile.Controls.Controls;

/// <summary>
/// A single dashboard KPI: a large brand-coloured value over a muted label, on a surface card. Mirrors the
/// console's KPI tiles so the manager overview reads as one system with the web dashboard. The value and label
/// are bindable so a page can update them live once the dashboard data loads.
/// </summary>
public class TwKpiCard : TwCard
{
    /// <summary>The headline value (e.g. a count or a percentage).</summary>
    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(string), typeof(TwKpiCard), "—");

    /// <summary>The label beneath the value.</summary>
    public static readonly BindableProperty LabelProperty =
        BindableProperty.Create(nameof(Label), typeof(string), typeof(TwKpiCard), string.Empty);

    /// <summary>The headline value.</summary>
    public string Value { get => (string)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>The supporting label.</summary>
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Composes the KPI layout and binds the value + label.</summary>
    public TwKpiCard()
    {
        var value = new Microsoft.Maui.Controls.Label { FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = TwPalette.Brand };
        value.SetBinding(Microsoft.Maui.Controls.Label.TextProperty, new Binding(nameof(Value), source: this));

        var label = new Microsoft.Maui.Controls.Label { FontSize = 13 };
        label.SetAppThemeColor(Microsoft.Maui.Controls.Label.TextColorProperty, TwPalette.TextSecondaryLight, TwPalette.TextSecondaryDark);
        label.SetBinding(Microsoft.Maui.Controls.Label.TextProperty, new Binding(nameof(Label), source: this));

        Content = new VerticalStackLayout { Spacing = 2, Children = { value, label } };
    }
}
