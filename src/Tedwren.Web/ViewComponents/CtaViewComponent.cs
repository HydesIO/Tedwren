using Microsoft.AspNetCore.Mvc;
using Tedwren.Web.Configuration;

namespace Tedwren.Web.ViewComponents;

/// <summary>
/// Renders one of the three canonical calls to action (Web Plan §5.4). It accepts a
/// <see cref="CtaAction"/> — a closed set — so button copy is always the literal next action and
/// vague labels ("Learn more", "Get started") cannot be introduced by a view author. An optional
/// style flag lets a page render the same action as a primary or secondary button without forking
/// the copy.
/// </summary>
public sealed class CtaViewComponent : ViewComponent
{
    /// <summary>Builds the CTA view model from a canonical action.</summary>
    /// <param name="action">The canonical action to render.</param>
    /// <param name="secondary">When true, render as a secondary button; otherwise primary.</param>
    public IViewComponentResult Invoke(CtaAction action, bool secondary = false)
    {
        var (label, href) = CtaCatalogue.Resolve(action);
        return View(new CtaModel(label, href, secondary));
    }
}

/// <summary>View model for a canonical CTA button.</summary>
/// <param name="Label">Fixed button copy.</param>
/// <param name="Href">Target path.</param>
/// <param name="Secondary">Whether to style as secondary.</param>
public sealed record CtaModel(string Label, string Href, bool Secondary);
