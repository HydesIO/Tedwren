using Microsoft.AspNetCore.Mvc;

namespace Tedwren.Web.Controllers;

/// <summary>Serves the Worker Passport page (Web Plan §4, §6.4) — the individual-buyer audience.</summary>
public sealed class WorkerPassportController : Controller
{
    /// <summary>Worker Passport page at <c>/worker-passport</c>.</summary>
    [HttpGet("/worker-passport")]
    public IActionResult Index() => View();
}
