using Microsoft.AspNetCore.Mvc;
using Tedwren.Abstractions.Contracts.WebContent;

namespace Tedwren.Web.ViewComponents;

/// <summary>Renders a grid of feature cards (Web Plan §5, §6). Reused across product pages and Home.</summary>
public sealed class FeatureGridViewComponent : ViewComponent
{
    /// <summary>Renders the supplied features as a grid.</summary>
    /// <param name="features">Feature cards to render.</param>
    public IViewComponentResult Invoke(IReadOnlyList<FeatureCard> features) => View(features);
}
