using Microsoft.AspNetCore.Mvc;

namespace Tedwren.Web.Controllers;

/// <summary>Serves the Security &amp; Trust page (Web Plan §4, §6.6) at a stable URL for procurement/DPO.</summary>
public sealed class TrustController : Controller
{
    /// <summary>Security &amp; Trust page at <c>/security</c>.</summary>
    [HttpGet("/security")]
    public IActionResult Index() => View();
}
