using Microsoft.AspNetCore.Mvc;

namespace Tedwren.Web.Controllers;

/// <summary>Serves the home page (Web Plan §4) and the shared error page.</summary>
public sealed class HomeController : Controller
{
    /// <summary>Home page at <c>/</c> — the audience split entry point.</summary>
    [HttpGet("/")]
    public IActionResult Index() => View();

    /// <summary>
    /// Shared error page target used by the exception handler and the status-code re-execution. Routed
    /// for all HTTP methods so a failed POST (e.g. antiforgery) re-executes here and keeps its status
    /// code, rather than 405-ing because the error route only allowed GET.
    /// </summary>
    [Route("/error")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Error() => View();
}
