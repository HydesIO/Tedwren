using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tedwren.Abstractions.Contracts.WebContent;
using Tedwren.Abstractions.Services;
using Tedwren.Web.Configuration;

namespace Tedwren.Web.ViewComponents;

/// <summary>
/// Renders the site footer (Web Plan §5): legal entity + company number/office and social icons from
/// the content layer (<see cref="IContentProvider"/>), and the site/legal link lists from
/// <see cref="SiteConfig"/>. Social icons render only for accounts that exist at launch, so the footer
/// never shows an icon for an account that does not exist.
/// </summary>
public sealed class SiteFooterViewComponent : ViewComponent
{
    private readonly SiteConfig _site;
    private readonly IContentProvider _content;

    /// <summary>Injects the navigation structure and the content layer.</summary>
    /// <param name="site">Bound "Site" navigation configuration.</param>
    /// <param name="content">Content provider for legal entity/social.</param>
    public SiteFooterViewComponent(IOptions<SiteConfig> site, IContentProvider content)
    {
        _site = site.Value;
        _content = content;
    }

    /// <summary>Builds the footer view model from content + nav config.</summary>
    public IViewComponentResult Invoke()
    {
        var identity = _content.Site;
        var model = new SiteFooterModel(
            identity.LegalEntity,
            identity.CompanyNumber,
            identity.RegisteredOffice,
            _site.FooterLinks,
            _site.LegalLinks,
            identity.Social,
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
    IReadOnlyList<SocialAccount> SocialLinks,
    int Year);
