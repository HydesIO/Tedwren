using Microsoft.AspNetCore.Mvc;

namespace Tedwren.Web.Controllers;

/// <summary>Serves the FAQ page (Web Plan §4, §6.8), which deflects to the relevant page.</summary>
public sealed class FaqController : Controller
{
    /// <summary>FAQ page at <c>/faq</c>.</summary>
    [HttpGet("/faq")]
    public IActionResult Index() => View();
}
