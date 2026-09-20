using System.Windows.Input;
using Tedwren.Mobile.Controls.Theme;

namespace Tedwren.Mobile.Controls.Controls;

/// <summary>
/// An interactive card tile for the role home "card menu": a glyph, a title and a short subtitle that runs a
/// <see cref="Command"/> when tapped. These make up the two distinct card-based menus (operative and manager).
/// </summary>
public class TwMenuTile : TwCard
{
    /// <summary>The tile's leading glyph (an emoji or icon character).</summary>
    public static readonly BindableProperty GlyphProperty =
        BindableProperty.Create(nameof(Glyph), typeof(string), typeof(TwMenuTile), string.Empty);

    /// <summary>The tile's title line.</summary>
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(TwMenuTile), string.Empty,
            propertyChanged: (b, _, _) => ((TwMenuTile)b).UpdateSemantics());

    /// <summary>The tile's supporting subtitle line.</summary>
    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(TwMenuTile), string.Empty,
            propertyChanged: (b, _, _) => ((TwMenuTile)b).UpdateSemantics());

    /// <summary>Command invoked when the tile is tapped.</summary>
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(TwMenuTile));

    /// <summary>Parameter passed to <see cref="Command"/>.</summary>
    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(TwMenuTile));

    /// <summary>The tile's leading glyph.</summary>
    public string Glyph { get => (string)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }

    /// <summary>The tile's title line.</summary>
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>The tile's supporting subtitle line.</summary>
    public string Subtitle { get => (string)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }

    /// <summary>Command invoked when the tile is tapped.</summary>
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    /// <summary>Parameter passed to <see cref="Command"/>.</summary>
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    /// <summary>Composes the tile layout and wires the tap gesture to the command.</summary>
    public TwMenuTile()
    {
        var glyph = new Label { FontSize = 28 };
        glyph.SetBinding(Label.TextProperty, new Binding(nameof(Glyph), source: this));

        var title = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold };
        title.SetBinding(Label.TextProperty, new Binding(nameof(Title), source: this));

        var subtitle = new Label { FontSize = 13, LineBreakMode = LineBreakMode.WordWrap };
        subtitle.SetAppThemeColor(Label.TextColorProperty, TwPalette.TextSecondaryLight, TwPalette.TextSecondaryDark);
        subtitle.SetBinding(Label.TextProperty, new Binding(nameof(Subtitle), source: this));

        Content = new VerticalStackLayout
        {
            Spacing = 6,
            Children = { glyph, title, subtitle },
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            if (Command?.CanExecute(CommandParameter) == true)
            {
                Command.Execute(CommandParameter);
            }
        };
        GestureRecognizers.Add(tap);
    }

    /// <summary>Announces the tile as one unit to assistive technology (title + subtitle), so it reads as a menu item.</summary>
    private void UpdateSemantics() =>
        SemanticProperties.SetDescription(this, string.IsNullOrEmpty(Subtitle) ? Title : $"{Title}. {Subtitle}");
}
