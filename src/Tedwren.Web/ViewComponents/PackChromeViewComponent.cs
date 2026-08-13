using Microsoft.AspNetCore.Mvc;
using Tedwren.Abstractions.Services;
using Tedwren.Web.Attribution;

namespace Tedwren.Web.ViewComponents;

/// <summary>
/// The light header + footer chrome for the product-owned compliance-pack viewer (Web Plan §4.1). The
/// pack viewer is served by the product, not this site, but must reuse this site's brand and a UTM-tagged
/// "Book a demo" link so pack-driven bookings are measurable (GTM Stage 2). There is deliberately no
/// sign-up wall. This component supplies that shared chrome from the same brand content.
/// </summary>
public sealed class PackChromeViewComponent : ViewComponent
{
    private readonly IContentProvider _content;

    /// <summary>Injects the content layer for brand identity.</summary>
    /// <param name="content">Content provider.</param>
    public PackChromeViewComponent(IContentProvider content) => _content = content;

    /// <summary>Builds the pack chrome model.</summary>
    /// <param name="footer">When true, render the footer variant; otherwise the header.</param>
    public IViewComponentResult Invoke(bool footer = false)
    {
        var model = new PackChromeModel(
            _content.Site.BrandName,
            _content.Site.LegalEntity,
            Utm.Pack.ToDemoUrl(),
            footer);
        return View(model);
    }
}

/// <summary>View model for the compliance-pack viewer chrome.</summary>
/// <param name="BrandName">Trading name for the light header.</param>
/// <param name="LegalEntity">Legal entity for the light footer.</param>
/// <param name="DemoUrl">UTM-tagged "Book a demo" URL (utm_source=pack).</param>
/// <param name="IsFooter">Whether this is the footer variant.</param>
public sealed record PackChromeModel(string BrandName, string LegalEntity, string DemoUrl, bool IsFooter);
