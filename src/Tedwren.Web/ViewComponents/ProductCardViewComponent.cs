using Microsoft.AspNetCore.Mvc;
using Tedwren.Abstractions.Services;

namespace Tedwren.Web.ViewComponents;

/// <summary>
/// Renders a product short-form as a linked card on the home page (Web Plan §6.1). It reads the same
/// <see cref="Tedwren.Abstractions.Contracts.WebContent.ProductProfile"/> entry as the dedicated page,
/// so there is no forked product copy — the card and the page can never drift.
/// </summary>
public sealed class ProductCardViewComponent : ViewComponent
{
    private readonly IContentProvider _content;

    /// <summary>Injects the content layer.</summary>
    /// <param name="content">Content provider.</param>
    public ProductCardViewComponent(IContentProvider content) => _content = content;

    /// <summary>Builds the card from a product key.</summary>
    /// <param name="configKey">Stable product key (e.g. "Subcontractor").</param>
    public IViewComponentResult Invoke(string configKey)
    {
        var product = _content.FindProduct(configKey)
            ?? throw new KeyNotFoundException($"Unknown product '{configKey}'.");
        return View(product);
    }
}
