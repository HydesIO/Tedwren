using Microsoft.AspNetCore.Mvc;

namespace Tedwren.Web.Controllers;

/// <summary>Serves the About page (Web Plan §4, §6.7) — founder-led credibility.</summary>
public sealed class AboutController : Controller
{
    /// <summary>About page at <c>/about</c>.</summary>
    [HttpGet("/about")]
    public IActionResult Index() => View();
}
