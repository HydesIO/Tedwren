using Microsoft.AspNetCore.Mvc;
using Tedwren.Abstractions.Services;

namespace Tedwren.Web.ViewComponents;

/// <summary>
/// Renders the FAQ list as a progressive-enhancement accordion (Web Plan §5, §6.8) using native
/// &lt;details&gt;/&lt;summary&gt; so it works with no JavaScript. FAQ items come from the content layer;
/// the FAQPage schema is emitted separately by the page.
/// </summary>
public sealed class FaqAccordionViewComponent : ViewComponent
{
    private readonly IContentProvider _content;

    /// <summary>Injects the content layer.</summary>
    /// <param name="content">Content provider.</param>
    public FaqAccordionViewComponent(IContentProvider content) => _content = content;

    /// <summary>Renders the configured FAQ items.</summary>
    public IViewComponentResult Invoke() => View(_content.Faqs);
}
