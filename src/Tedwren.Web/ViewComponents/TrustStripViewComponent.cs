using Microsoft.AspNetCore.Mvc;
using Tedwren.Abstractions.Services;

namespace Tedwren.Web.ViewComponents;

/// <summary>
/// Renders the trust strip (Web Plan §5.3) — one component referenced on Home and the product pages,
/// expanded on `/security`, never copy-pasted. Trust points come from the content layer.
/// </summary>
public sealed class TrustStripViewComponent : ViewComponent
{
    private readonly IContentProvider _content;

    /// <summary>Injects the content layer.</summary>
    /// <param name="content">Content provider.</param>
    public TrustStripViewComponent(IContentProvider content) => _content = content;

    /// <summary>Renders the configured trust points.</summary>
    public IViewComponentResult Invoke() => View(_content.TrustPoints);
}
