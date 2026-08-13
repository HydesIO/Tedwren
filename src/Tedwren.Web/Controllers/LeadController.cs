using Microsoft.AspNetCore.Mvc;

namespace Tedwren.Web.Controllers;

/// <summary>
/// Serves the two lead-capture pages (Web Plan §4, §7). W1 renders stub pages only; the validated
/// forms, routing and calendar integration are added in W6.
/// </summary>
public sealed class LeadController : Controller
{
    /// <summary>Book a demo / pilot page at <c>/demo</c>.</summary>
    [HttpGet("/demo")]
    public IActionResult Demo() => View();

    /// <summary>Contact page at <c>/contact</c>.</summary>
    [HttpGet("/contact")]
    public IActionResult Contact() => View();
}
