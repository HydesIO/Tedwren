using Microsoft.AspNetCore.Mvc;

namespace Tedwren.Web.Controllers;

/// <summary>
/// Serves the two audience product pages (Web Plan §4). Routes use audience/function slugs so a
/// product rename is a content + redirect change, not a route change (Web Plan §3).
/// </summary>
public sealed class ProductsController : Controller
{
    /// <summary>Subcontractor product page — the primary launch landing page (Web Plan §6.2).</summary>
    [HttpGet("/subcontractors")]
    public IActionResult Subcontractor() => View();

    /// <summary>Main contractor product page (Web Plan §6.3).</summary>
    [HttpGet("/main-contractors")]
    public IActionResult MainContractor() => View();
}
