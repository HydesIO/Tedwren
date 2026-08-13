using Microsoft.AspNetCore.Mvc;

namespace Tedwren.Web.Controllers;

/// <summary>Serves the pricing page (Web Plan §4, §6.5). All numbers come from content, not views.</summary>
public sealed class PricingController : Controller
{
    /// <summary>Pricing page at <c>/pricing</c>.</summary>
    [HttpGet("/pricing")]
    public IActionResult Index() => View();
}
