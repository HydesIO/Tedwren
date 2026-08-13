using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tedwren.Abstractions.Services;
using Tedwren.Web.Configuration;

namespace Tedwren.Web.ViewComponents;

/// <summary>
/// Renders the site header (Web Plan §5): logo → home, primary nav, and a persistent top-right CTA.
/// The brand name comes from the content layer (<see cref="IContentProvider"/>); the nav structure
/// comes from <see cref="SiteConfig"/>. The CTA is "Book a demo" everywhere except the Worker Passport
/// page, where it swaps to "Get your Worker Passport".
/// </summary>
public sealed class SiteHeaderViewComponent : ViewComponent
{
    private readonly SiteConfig _site;
    private readonly IContentProvider _content;

    /// <summary>Injects the navigation structure and the content layer.</summary>
    /// <param name="site">Bound "Site" navigation configuration.</param>
    /// <param name="content">Content provider for brand/identity.</param>
    public SiteHeaderViewComponent(IOptions<SiteConfig> site, IContentProvider content)
    {
        _site = site.Value;
        _content = content;
    }

    /// <summary>Builds the header view model, choosing the CTA from the current path.</summary>
    public IViewComponentResult Invoke()
    {
        var path = HttpContext.Request.Path.Value ?? "/";
        var cta = path.StartsWith("/worker-passport", StringComparison.OrdinalIgnoreCase)
            ? CtaAction.GetWorkerPassport
            : CtaAction.BookDemo;

        var model = new SiteHeaderModel(_content.Site.BrandName, _site.PrimaryNav, cta);
        return View(model);
    }
}

/// <summary>View model for the site header.</summary>
/// <param name="BrandName">Trading name shown by the logo/wordmark.</param>
/// <param name="PrimaryNav">Ordered primary navigation links.</param>
/// <param name="Cta">The canonical CTA to render top-right.</param>
public sealed record SiteHeaderModel(string BrandName, IReadOnlyList<NavLink> PrimaryNav, CtaAction Cta);
