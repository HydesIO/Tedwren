using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Tedwren.Web.Leads;
using Tedwren.Web.Models;

namespace Tedwren.Web.Controllers;

/// <summary>
/// Captures pre-launch interest emails (Web Content Spec §6.9). The signup form is shown on the home page and
/// on a standalone page; a genuine submission is forwarded to the API's launch list via
/// <see cref="ILaunchSignupSink"/> and the visitor is redirected to a thank-you page (post/redirect/get).
/// Server-validated with antiforgery, a honeypot, a minimum fill-time and rate limiting, like the lead forms.
/// </summary>
public sealed class LaunchController : Controller
{
    private readonly ILaunchSignupSink _sink;
    private readonly LeadOptions _leadOptions;

    /// <summary>Injects the launch-signup sink and the (shared) lead options for the anti-bot fill time.</summary>
    public LaunchController(ILaunchSignupSink sink, IOptions<LeadOptions> leadOptions)
    {
        _sink = sink;
        _leadOptions = leadOptions.Value;
    }

    /// <summary>Standalone "join the launch list" page at <c>/launch</c>.</summary>
    [HttpGet("/launch")]
    public IActionResult Index() => View(new LaunchSignupRequest { RenderedAtUnixMs = Now() });

    /// <summary>Processes a launch-list signup: validate, anti-bot, forward, then redirect to thanks.</summary>
    [HttpPost("/launch")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(LeadController.RateLimitPolicy)]
    public async Task<IActionResult> Index(LaunchSignupRequest model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.RenderedAtUnixMs = Now(); // fresh render token for the re-shown form
            return View(model);
        }

        if (AntiBot.IsHuman(model, Now(), _leadOptions.MinFormSeconds))
        {
            await _sink.SubmitAsync(model.Email, "landing", cancellationToken);
        }

        // Redirect either way — a dropped bot submission gets the same response, revealing nothing.
        return RedirectToAction(nameof(Thanks));
    }

    /// <summary>Launch-list thank-you page.</summary>
    [HttpGet("/launch/thanks")]
    public IActionResult Thanks() => View();

    /// <summary>Current time in Unix milliseconds (for the anti-bot render token).</summary>
    private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
