using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tedwren.Web.Configuration;

namespace Tedwren.Web.ViewComponents;

/// <summary>
/// Renders the site header (Web Plan §5): logo → home, primary nav from config, and a persistent
/// top-right CTA. The CTA is "Book a demo" everywhere except the Worker Passport page, where it
/// swaps to "Get your Worker Passport". Nav labels/targets come from <see cref="SiteConfig"/>, never
/// from the view.
/// </summary>
public sealed class SiteHeaderViewComponent : ViewComponent
{
    private readonly SiteConfig _site;

    /// <summary>Injects the config-bound site chrome.</summary>
    /// <param name="site">Bound "Site" configuration.</param>
    public SiteHeaderViewComponent(IOptions<SiteConfig> site) => _site = site.Value;

    /// <summary>Builds the header view model, choosing the CTA from the current path.</summary>
    public IViewComponentResult Invoke()
    {
        var path = HttpContext.Request.Path.Value ?? "/";
        var cta = path.StartsWith("/worker-passport", StringComparison.OrdinalIgnoreCase)
            ? CtaAction.GetWorkerPassport
            : CtaAction.BookDemo;

        var model = new SiteHeaderModel(_site.BrandName, _site.PrimaryNav, cta);
        return View(model);
    }
}

/// <summary>View model for the site header.</summary>
/// <param name="BrandName">Trading name shown by the logo/wordmark.</param>
/// <param name="PrimaryNav">Ordered primary navigation links.</param>
/// <param name="Cta">The canonical CTA to render top-right.</param>
public sealed record SiteHeaderModel(string BrandName, IReadOnlyList<NavLink> PrimaryNav, CtaAction Cta);
