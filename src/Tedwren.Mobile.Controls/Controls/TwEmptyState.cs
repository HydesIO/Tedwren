using Tedwren.Mobile.Controls.Theme;

namespace Tedwren.Mobile.Controls.Controls;

/// <summary>
/// A centred, muted empty-state message with an optional glyph — shown when a list has no items or a screen has
/// nothing to display yet. Keeps "nothing here" screens deliberate rather than blank, matching the console's
/// empty-state treatment.
/// </summary>
public class TwEmptyState : ContentView
{
    /// <summary>A leading glyph (emoji/icon character); optional.</summary>
    public static readonly BindableProperty GlyphProperty =
        BindableProperty.Create(nameof(Glyph), typeof(string), typeof(TwEmptyState), string.Empty);

    /// <summary>The empty-state message.</summary>
    public static readonly BindableProperty MessageProperty =
        BindableProperty.Create(nameof(Message), typeof(string), typeof(TwEmptyState), "Nothing to show yet.");

    /// <summary>A leading glyph.</summary>
    public string Glyph { get => (string)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }

    /// <summary>The empty-state message.</summary>
    public string Message { get => (string)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }

    /// <summary>Composes the centred glyph + message.</summary>
    public TwEmptyState()
    {
        var glyph = new Label { FontSize = 34, HorizontalOptions = LayoutOptions.Center };
        glyph.SetBinding(Label.TextProperty, new Binding(nameof(Glyph), source: this));
        AutomationProperties.SetIsInAccessibleTree(glyph, false); // decorative — the message carries the meaning

        var message = new Label { FontSize = 14, HorizontalTextAlignment = TextAlignment.Center, HorizontalOptions = LayoutOptions.Center };
        message.SetAppThemeColor(Label.TextColorProperty, TwPalette.TextSecondaryLight, TwPalette.TextSecondaryDark);
        message.SetBinding(Label.TextProperty, new Binding(nameof(Message), source: this));

        Content = new VerticalStackLayout
        {
            Spacing = 8,
            Padding = new Thickness(24, 40),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children = { glyph, message },
        };
    }
}
