using MudBlazor;

namespace Tedwren.UiComponents.Theme;

/// <summary>
/// The single source of truth for MudBlazor theming, generated directly from the
/// design tokens in <c>wwwroot/css/tokens.css</c> (Plan &amp; Scope §4). Applied once at
/// the <c>MudThemeProvider</c> level and never overridden per page, so Razor markup and
/// MudBlazor's own internals never disagree on colour, radius or typography.
/// </summary>
public static class TedwrenTheme
{
    // --- Raw token values (kept in lock-step with tokens.css) ------------------
    private const string ColorBg = "#F7F8FA";
    private const string ColorSurface = "#FFFFFF";
    private const string ColorTextPrimary = "#101828";
    private const string ColorTextSecondary = "#667085";
    private const string ColorTextMuted = "#98A2B3";
    private const string ColorBorder = "#E4E7EC";

    private const string ColorBrand = "#E8590C";
    private const string ColorBrandDark = "#B8430C";

    private const string ColorSuccess = "#17B26A";
    private const string ColorWarning = "#F79009";
    private const string ColorDanger = "#F04438";
    private const string ColorInfo = "#2E90FA";

    private const string FontFamily = "Inter";

    /// <summary>The one <see cref="MudTheme"/> instance used across the whole app.</summary>
    public static readonly MudTheme Instance = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = ColorBrand,
            PrimaryDarken = ColorBrandDark,
            Secondary = ColorTextSecondary,
            Success = ColorSuccess,
            Warning = ColorWarning,
            Error = ColorDanger,
            Info = ColorInfo,

            Background = ColorBg,
            Surface = ColorSurface,
            AppbarBackground = ColorSurface,
            AppbarText = ColorTextPrimary,
            DrawerBackground = ColorSurface,
            DrawerText = ColorTextPrimary,

            TextPrimary = ColorTextPrimary,
            TextSecondary = ColorTextSecondary,
            TextDisabled = ColorTextMuted,

            LinesDefault = ColorBorder,
            LinesInputs = ColorBorder,
            TableLines = ColorBorder,
            Divider = ColorBorder,

            DrawerIcon = ColorTextSecondary,
        },

        LayoutProperties = new LayoutProperties
        {
            // --radius-card 10px is the default; controls (--radius-control 8px) are
            // handled at the component/CSS layer to avoid rounding cards like buttons.
            DefaultBorderRadius = "10px",
            DrawerWidthLeft = "260px",
            AppbarHeight = "64px",
        },

        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = new[] { FontFamily, "-apple-system", "BlinkMacSystemFont", "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif" },
                FontSize = "0.875rem",
                FontWeight = "400",
                LineHeight = "1.5",
            },
            H1 = new H1Typography { FontFamily = new[] { FontFamily, "sans-serif" }, FontSize = "1.75rem", FontWeight = "600", LineHeight = "1.3" },
            H2 = new H2Typography { FontFamily = new[] { FontFamily, "sans-serif" }, FontSize = "1.5rem", FontWeight = "600", LineHeight = "1.3" },
            H3 = new H3Typography { FontFamily = new[] { FontFamily, "sans-serif" }, FontSize = "1.25rem", FontWeight = "600", LineHeight = "1.35" },
            H4 = new H4Typography { FontFamily = new[] { FontFamily, "sans-serif" }, FontSize = "1.125rem", FontWeight = "600", LineHeight = "1.4" },
            H5 = new H5Typography { FontFamily = new[] { FontFamily, "sans-serif" }, FontSize = "1rem", FontWeight = "600", LineHeight = "1.4" },
            H6 = new H6Typography { FontFamily = new[] { FontFamily, "sans-serif" }, FontSize = "0.9375rem", FontWeight = "600", LineHeight = "1.4" },
            Body1 = new Body1Typography { FontFamily = new[] { FontFamily, "sans-serif" }, FontSize = "0.875rem", FontWeight = "400", LineHeight = "1.5" },
            Body2 = new Body2Typography { FontFamily = new[] { FontFamily, "sans-serif" }, FontSize = "0.8125rem", FontWeight = "400", LineHeight = "1.5" },
            Button = new ButtonTypography { FontFamily = new[] { FontFamily, "sans-serif" }, FontSize = "0.875rem", FontWeight = "600", TextTransform = "none" },
            Caption = new CaptionTypography { FontFamily = new[] { FontFamily, "sans-serif" }, FontSize = "0.75rem", FontWeight = "400", LineHeight = "1.4" },
        },
    };
}
