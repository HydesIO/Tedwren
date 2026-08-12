namespace Tedwren.Application.Notifications.Email;

/// <summary>
/// The single source of colour, spacing and typography values for the HTML email template and its components.
/// Keeping every literal here (mirroring the client's <c>tokens.css</c> discipline) means the branded emails
/// stay visually consistent and can be re-themed in one place. Values are plain strings so they drop straight
/// into inline styles, which is the only styling email clients reliably honour.
/// </summary>
internal static class EmailPalette
{
    /// <summary>Tedwren brand orange (matches the logo and the client theme).</summary>
    public const string Brand = "#E8590C";

    /// <summary>A darker brand shade for borders/accents.</summary>
    public const string BrandDark = "#D9480F";

    /// <summary>The page background behind the content container.</summary>
    public const string PageBackground = "#F4F5F7";

    /// <summary>The content container background.</summary>
    public const string ContainerBackground = "#FFFFFF";

    /// <summary>Primary body text.</summary>
    public const string Text = "#212529";

    /// <summary>Muted/secondary text (footer, captions).</summary>
    public const string Muted = "#6C757D";

    /// <summary>Hairline borders (tables, dividers, container edge).</summary>
    public const string Border = "#E9ECEF";

    /// <summary>Background for highlighted callouts.</summary>
    public const string CalloutBackground = "#FFF4E6";

    /// <summary>Left accent border for highlighted callouts.</summary>
    public const string CalloutAccent = "#FFA94D";

    /// <summary>Table header row background.</summary>
    public const string TableHeaderBackground = "#F8F9FA";

    /// <summary>Button label colour (on the brand background).</summary>
    public const string ButtonText = "#FFFFFF";

    /// <summary>The web-safe font stack applied throughout.</summary>
    public const string FontFamily = "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";

    /// <summary>Width of the content container in pixels.</summary>
    public const int ContainerWidth = 600;
}
