namespace Tedwren.Mobile.Controls.Theme;

/// <summary>
/// C# mirror of the Tedwren colour tokens (see Resources/TedwrenColors.xaml, itself ported from tokens.css) for
/// use in code-defined controls where a XAML StaticResource is awkward. This is the single code-side source of
/// these values — keep it in lock-step with the XAML tokens, exactly as the web keeps tokens.css and the
/// generated TedwrenTheme.cs in step. No colour literals elsewhere in the control kit.
/// </summary>
public static class TwPalette
{
    /// <summary>Brand orange (identical in both themes).</summary>
    public static readonly Color Brand = Color.FromArgb("#E8590C");

    /// <summary>Foreground on the brand colour.</summary>
    public static readonly Color OnBrand = Colors.White;

    /// <summary>Brand pale — light / dark.</summary>
    public static readonly Color BrandPaleLight = Color.FromArgb("#FFF1E8");
    public static readonly Color BrandPaleDark = Color.FromArgb("#3A2317");

    /// <summary>Card / surface background — light / dark.</summary>
    public static readonly Color SurfaceLight = Color.FromArgb("#FFFFFF");
    public static readonly Color SurfaceDark = Color.FromArgb("#161B26");

    /// <summary>Hairline border — light / dark.</summary>
    public static readonly Color BorderLight = Color.FromArgb("#E4E7EC");
    public static readonly Color BorderDark = Color.FromArgb("#2A3140");

    /// <summary>Primary text — light / dark.</summary>
    public static readonly Color TextPrimaryLight = Color.FromArgb("#101828");
    public static readonly Color TextPrimaryDark = Color.FromArgb("#F5F6F8");

    /// <summary>Secondary text — light / dark.</summary>
    public static readonly Color TextSecondaryLight = Color.FromArgb("#667085");
    public static readonly Color TextSecondaryDark = Color.FromArgb("#94A0B4");
}
