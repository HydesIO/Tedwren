using Microsoft.AspNetCore.Mvc;
using Tedwren.Abstractions.Services;

namespace Tedwren.Web.ViewComponents;

/// <summary>
/// Renders a product long-form on its dedicated page (Web Plan §6.2, §6.3): hero, feature grid and any
/// page-specific narrative sections (the understated CSCS line, the substantial retrofit section). It
/// reads the same <see cref="Tedwren.Abstractions.Contracts.WebContent.ProductProfile"/> entry as the
/// home card, so there is no forked product copy.
/// </summary>
public sealed class ProductDetailViewComponent : ViewComponent
{
    private readonly IContentProvider _content;

    /// <summary>Injects the content layer.</summary>
    /// <param name="content">Content provider.</param>
    public ProductDetailViewComponent(IContentProvider content) => _content = content;

    /// <summary>Builds the detail view from a product key.</summary>
    /// <param name="configKey">Stable product key (e.g. "Subcontractor").</param>
    public IViewComponentResult Invoke(string configKey)
    {
        var product = _content.FindProduct(configKey)
            ?? throw new KeyNotFoundException($"Unknown product '{configKey}'.");
        return View(product);
    }
}
