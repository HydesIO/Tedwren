using Microsoft.AspNetCore.Mvc;
using Tedwren.Abstractions.Services;

namespace Tedwren.Web.ViewComponents;

/// <summary>
/// Renders the pricing bands (Web Plan §5, §6.5). Every number comes from the <see cref="IContentProvider"/>
/// pricing plans — none is typed into the view (Plan §8). Prices are formatted here in the site currency;
/// a plan with no set price yet (an open sign-off item, Plan §11.2) renders "Pricing on request" rather
/// than a fabricated number.
/// </summary>
public sealed class PricingTableViewComponent : ViewComponent
{
    private readonly IContentProvider _content;

    /// <summary>Injects the content layer.</summary>
    /// <param name="content">Content provider.</param>
    public PricingTableViewComponent(IContentProvider content) => _content = content;

    /// <summary>Builds pre-formatted pricing rows from the configured plans.</summary>
    public IViewComponentResult Invoke()
    {
        var rows = _content.PricingPlans
            .Select(p => new PricingRow(
                p.Name,
                p.BandDescription,
                p.AnnualPrice > 0 ? _content.FormatMoney(p.AnnualPrice) : "Pricing on request",
                p.MonthlyPrice is { } m && m > 0 ? _content.FormatMoney(m) : null))
            .ToList();

        return View(rows);
    }
}

/// <summary>A single pre-formatted pricing row for display.</summary>
/// <param name="Name">Plan name.</param>
/// <param name="Band">Plain-language band/unit description.</param>
/// <param name="AnnualDisplay">Formatted annual price, or "Pricing on request".</param>
/// <param name="MonthlyDisplay">Formatted monthly price, or null when not applicable.</param>
public sealed record PricingRow(string Name, string Band, string AnnualDisplay, string? MonthlyDisplay);
