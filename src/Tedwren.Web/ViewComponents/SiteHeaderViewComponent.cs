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

    /// <summary>Builds the header view model, choosing the CTA and the active nav item from the path.</summary>
    public IViewComponentResult Invoke()
    {
        var path = HttpContext.Request.Path.Value ?? "/";
        var cta = path.StartsWith("/worker-passport", StringComparison.OrdinalIgnoreCase)
            ? CtaAction.GetWorkerPassport
            : CtaAction.BookDemo;

        var model = new SiteHeaderModel(
            _content.Site.BrandName, _site.PrimaryNav, cta, ResolveActiveHref(_site.PrimaryNav, path));
        return View(model);
    }

    /// <summary>
    /// Chooses which nav link is "current" for the given request path so exactly one item is always
    /// selected. A non-root link matches when the path equals it or begins with it plus "/", and the
    /// longest such match wins (so "/subcontractors/x" prefers "For Subcontractors" over "Home"). When
    /// nothing else matches — the home page and any off-nav page (FAQ, Trust, Partners, legal, contact,
    /// lead flows) — it falls back to the root ("/") link, which is the Home item.
    /// </summary>
    /// <param name="nav">The primary navigation links.</param>
    /// <param name="path">The current request path.</param>
    private static string ResolveActiveHref(IReadOnlyList<NavLink> nav, string path)
    {
        string? best = null;
        foreach (var link in nav)
        {
            var href = link.Href;
            if (href == "/")
            {
                continue; // the root is the fallback, considered only if nothing more specific matches
            }

            var matches = path.Equals(href, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(href + "/", StringComparison.OrdinalIgnoreCase);
            if (matches && (best is null || href.Length > best.Length))
            {
                best = href;
            }
        }

        return best ?? "/";
    }
}

/// <summary>View model for the site header.</summary>
/// <param name="BrandName">Trading name shown by the logo/wordmark.</param>
/// <param name="PrimaryNav">Ordered primary navigation links.</param>
/// <param name="Cta">The canonical CTA to render top-right.</param>
/// <param name="ActiveHref">Href of the nav link to mark as the current page (always exactly one).</param>
public sealed record SiteHeaderModel(
    string BrandName, IReadOnlyList<NavLink> PrimaryNav, CtaAction Cta, string ActiveHref);
