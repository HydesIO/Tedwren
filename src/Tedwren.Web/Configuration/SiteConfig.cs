namespace Tedwren.Web.Configuration;

/// <summary>
/// Config-driven site chrome and identity, bound from the "Site" configuration section. This is the
/// W1 seam for the wider content layer: header, footer and CTAs read every customer-visible string
/// (product names, legal entity, nav labels) from here rather than from a <c>.cshtml</c> file, so a
/// rename or copy change is a configuration edit, not a redeploy (Web Plan §2, §3). W2 replaces the
/// static-config source behind an <c>IContentProvider</c> without changing the views.
/// </summary>
public sealed class SiteConfig
{
    /// <summary>Configuration section name this options type binds from.</summary>
    public const string SectionName = "Site";

    /// <summary>Trading name shown in the header and titles (e.g. "Tedwren").</summary>
    public string BrandName { get; set; } = "Tedwren";

    /// <summary>Registered legal entity for the footer (e.g. "Tedwren Ltd").</summary>
    public string LegalEntity { get; set; } = "Tedwren Ltd";

    /// <summary>Companies House registered company number, shown in the footer (Web Plan §12.6).</summary>
    public string? CompanyNumber { get; set; }

    /// <summary>Registered office address, shown in the footer.</summary>
    public string? RegisteredOffice { get; set; }

    /// <summary>Primary navigation items rendered by the header, in order.</summary>
    public List<NavLink> PrimaryNav { get; set; } = new();

    /// <summary>Site link list rendered by the footer, in order.</summary>
    public List<NavLink> FooterLinks { get; set; } = new();

    /// <summary>Legal link list rendered by the footer, in order.</summary>
    public List<NavLink> LegalLinks { get; set; } = new();

    /// <summary>
    /// Social links to render as footer icons. Only accounts that exist at launch are configured, so
    /// the footer never shows an icon for an account that does not exist (Web Plan §5, §11 open item 6).
    /// </summary>
    public List<SocialLink> SocialLinks { get; set; } = new();
}

/// <summary>A single navigation link: a display label and the path it points at.</summary>
public sealed class NavLink
{
    /// <summary>Visible link text.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Target path (site-relative, e.g. "/pricing").</summary>
    public string Href { get; set; } = string.Empty;
}

/// <summary>A configured social account link, rendered as a footer icon only when present.</summary>
public sealed class SocialLink
{
    /// <summary>Platform key used to pick the icon (e.g. "linkedin").</summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>Absolute URL to the account.</summary>
    public string Url { get; set; } = string.Empty;
}
