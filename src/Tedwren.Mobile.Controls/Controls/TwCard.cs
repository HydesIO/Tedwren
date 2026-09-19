using Microsoft.Maui.Controls.Shapes;
using Tedwren.Mobile.Controls.Theme;

namespace Tedwren.Mobile.Controls.Controls;

/// <summary>
/// A surface card — rounded (10px), hairline-bordered, padded container that flips with the theme, mirroring
/// the console's DashboardCard. Place page content inside it; it derives from <see cref="Border"/> so a single
/// child is set as its content.
/// </summary>
public class TwCard : Border
{
    /// <summary>Builds the card chrome (radius, border, surface background) from the design tokens.</summary>
    public TwCard()
    {
        Padding = new Thickness(16);
        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) };
        StrokeThickness = 1;
        this.SetAppThemeColor(BackgroundColorProperty, TwPalette.SurfaceLight, TwPalette.SurfaceDark);
        this.SetAppTheme<Brush>(
            StrokeProperty,
            new SolidColorBrush(TwPalette.BorderLight),
            new SolidColorBrush(TwPalette.BorderDark));
    }
}
