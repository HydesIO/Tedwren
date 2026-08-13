using Microsoft.AspNetCore.Mvc;
using Tedwren.Abstractions.Services;

namespace Tedwren.Web.ViewComponents;

/// <summary>
/// Renders the home page's differentiator cards (Web Plan §6.1). The highlighted card ("Works beyond
/// the site gate") is given a distinct visual treatment as the strongest differentiator.
/// </summary>
public sealed class DifferentiatorsViewComponent : ViewComponent
{
    private readonly IContentProvider _content;

    /// <summary>Injects the content layer.</summary>
    /// <param name="content">Content provider.</param>
    public DifferentiatorsViewComponent(IContentProvider content) => _content = content;

    /// <summary>Renders the configured differentiators.</summary>
    public IViewComponentResult Invoke() => View(_content.Home.Differentiators);
}
