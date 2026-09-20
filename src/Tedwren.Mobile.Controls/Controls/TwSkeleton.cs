using Microsoft.Maui.Controls.Shapes;
using Tedwren.Mobile.Controls.Theme;

namespace Tedwren.Mobile.Controls.Controls;

/// <summary>
/// A loading placeholder (M8): a rounded, theme-aware bar that gently pulses while content loads — the design
/// system's "skeletons not spinners" convention. Give it a <see cref="Microsoft.Maui.Controls.VisualElement.HeightRequest"/>
/// (and optionally width); it animates while attached and stops when removed. It is decorative, so it is hidden
/// from assistive technology.
/// </summary>
public class TwSkeleton : Border
{
    private bool _running;

    /// <summary>Builds the skeleton chrome (rounded, subtle surface) and marks it decorative for screen readers.</summary>
    public TwSkeleton()
    {
        HeightRequest = 16;
        StrokeThickness = 0;
        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(6) };
        this.SetAppThemeColor(BackgroundColorProperty, TwPalette.BorderLight, TwPalette.BorderDark);
        SemanticProperties.SetDescription(this, string.Empty);
        AutomationProperties.SetIsInAccessibleTree(this, false);

        Loaded += (_, _) => _ = PulseAsync();
        Unloaded += (_, _) => _running = false;
    }

    private async Task PulseAsync()
    {
        if (_running)
        {
            return;
        }

        _running = true;
        while (_running)
        {
            await this.FadeTo(0.45, 650, Easing.SinInOut);
            await this.FadeTo(1.0, 650, Easing.SinInOut);
        }
    }
}
