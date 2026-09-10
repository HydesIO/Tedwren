namespace Tedwren.Web.Configuration;

/// <summary>
/// Site navigation structure, bound from the "Site" configuration section: which pages appear in the
/// header, footer and legal link lists, and in what order. This is app/routing structure, not
/// marketing copy — every customer-visible string (brand, legal entity, product names, prices) comes
/// from the content layer (<c>IContentProvider</c>, Web Plan §3), not from here.
/// </summary>
public sealed class SiteConfig
{
    /// <summary>Configuration section name this options type binds from.</summary>
    public const string SectionName = "Site";

    /// <summary>
    /// Pre-launch landing gate. When true, the whole marketing site is replaced by a single inviting
    /// landing page (logo, marketing hooks and email capture); every other route funnels to it. Static
    /// assets, the landing form POST and the consent/health endpoints stay reachable so the page works.
    /// Defaults to false — the full site is served.
    /// </summary>
    public bool IsLanding { get; set; }

    /// <summary>Primary navigation items rendered by the header, in order.</summary>
    public List<NavLink> PrimaryNav { get; set; } = new();

    /// <summary>Site link list rendered by the footer, in order.</summary>
    public List<NavLink> FooterLinks { get; set; } = new();

    /// <summary>Legal link list rendered by the footer, in order.</summary>
    public List<NavLink> LegalLinks { get; set; } = new();
}

/// <summary>A single navigation link: a display label and the path it points at.</summary>
public sealed class NavLink
{
    /// <summary>Visible link text.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Target path (site-relative, e.g. "/pricing").</summary>
    public string Href { get; set; } = string.Empty;
}
