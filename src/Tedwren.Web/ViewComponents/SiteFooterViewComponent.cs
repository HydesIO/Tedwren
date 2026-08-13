using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tedwren.Web.Configuration;

namespace Tedwren.Web.ViewComponents;

/// <summary>
/// Renders the site footer (Web Plan §5): legal entity + company number/office, site links, legal
/// links, and social icons — but only for accounts that exist at launch, so the footer never shows
/// an icon for an account that does not exist. Everything is sourced from <see cref="SiteConfig"/>.
/// </summary>
public sealed class SiteFooterViewComponent : ViewComponent
{
    private readonly SiteConfig _site;

    /// <summary>Injects the config-bound site chrome.</summary>
    /// <param name="site">Bound "Site" configuration.</param>
    public SiteFooterViewComponent(IOptions<SiteConfig> site) => _site = site.Value;

    /// <summary>Builds the footer view model from config.</summary>
    public IViewComponentResult Invoke()
    {
        var model = new SiteFooterModel(
            _site.LegalEntity,
            _site.CompanyNumber,
            _site.RegisteredOffice,
            _site.FooterLinks,
            _site.LegalLinks,
            _site.SocialLinks,
            DateTime.UtcNow.Year);
        return View(model);
    }
}

/// <summary>View model for the site footer.</summary>
/// <param name="LegalEntity">Registered legal entity name.</param>
/// <param name="CompanyNumber">Companies House number, if configured.</param>
/// <param name="RegisteredOffice">Registered office address, if configured.</param>
/// <param name="FooterLinks">Ordered site link list.</param>
/// <param name="LegalLinks">Ordered legal link list.</param>
/// <param name="SocialLinks">Social accounts that exist at launch (may be empty).</param>
/// <param name="Year">Current year for the copyright line.</param>
public sealed record SiteFooterModel(
    string LegalEntity,
    string? CompanyNumber,
    string? RegisteredOffice,
    IReadOnlyList<NavLink> FooterLinks,
    IReadOnlyList<NavLink> LegalLinks,
    IReadOnlyList<SocialLink> SocialLinks,
    int Year);
