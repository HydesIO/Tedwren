using Microsoft.AspNetCore.Mvc;

namespace Tedwren.Web.ViewComponents;

/// <summary>
/// Renders a token-driven "product frame" — an illustrative snapshot of the product UI wrapped in
/// browser/phone chrome, used across the marketing site as a screenshot substitute (no raster assets,
/// theme-aware, crisp at any density). The composition is illustrative chrome, not customer copy, so it
/// carries no configurable strings and never a price literal; a single <see cref="ProductFrameVariant"/>
/// selects which snapshot to draw. Each frame is decorative-but-informative: the template wraps it with
/// <c>role="img"</c> and an <c>aria-label</c> summarising what it shows.
/// </summary>
public sealed class ProductFrameViewComponent : ViewComponent
{
    /// <summary>Builds the view model for the requested frame variant.</summary>
    /// <param name="variant">Which product snapshot to render.</param>
    /// <param name="caption">Optional caption rendered beneath the frame.</param>
    public IViewComponentResult Invoke(ProductFrameVariant variant, string? caption = null) =>
        View(new ProductFrameModel(variant, caption));
}

/// <summary>The set of product snapshots the marketing site can render (Web redesign product frames).</summary>
public enum ProductFrameVariant
{
    /// <summary>A live site register: workers signing in with compliance status chips and a running clock.</summary>
    SiteRegister,

    /// <summary>A workforce dashboard: compliance KPIs and an attendance trend.</summary>
    Dashboard,

    /// <summary>A shareable compliance pack sheet a recipient opens without an account.</summary>
    CompliancePack,

    /// <summary>A phone-framed pre-arrival induction / sign-in step.</summary>
    Induction,
}

/// <summary>View model for a product frame.</summary>
/// <param name="Variant">Which snapshot to render.</param>
/// <param name="Caption">Optional caption beneath the frame.</param>
public sealed record ProductFrameModel(ProductFrameVariant Variant, string? Caption);
