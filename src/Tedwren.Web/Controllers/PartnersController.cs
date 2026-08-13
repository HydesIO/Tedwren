using Microsoft.AspNetCore.Mvc;

namespace Tedwren.Web.Controllers;

/// <summary>
/// Serves the partner programme page (Web Plan §4, §7). The approval-gated application, attribution
/// and dashboard are built in W7; W1 renders the stub page.
/// </summary>
public sealed class PartnersController : Controller
{
    /// <summary>Partners / affiliates page at <c>/partners</c>.</summary>
    [HttpGet("/partners")]
    public IActionResult Index() => View();
}
