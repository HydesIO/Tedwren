using Microsoft.AspNetCore.Mvc;
using Tedwren.Abstractions.Services;

namespace Tedwren.Web.ViewComponents;

/// <summary>Renders the home page's five-step "how it works" sequence (Web Plan §6.1) from content.</summary>
public sealed class HowItWorksViewComponent : ViewComponent
{
    private readonly IContentProvider _content;

    /// <summary>Injects the content layer.</summary>
    /// <param name="content">Content provider.</param>
    public HowItWorksViewComponent(IContentProvider content) => _content = content;

    /// <summary>Renders the ordered steps.</summary>
    public IViewComponentResult Invoke() =>
        View(_content.Home.HowItWorks.OrderBy(s => s.Order).ToList());
}
