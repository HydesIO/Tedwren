using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Tedwren.Web.Leads;
using Tedwren.Web.Models;

namespace Tedwren.Web.Controllers;

/// <summary>
/// Serves the pre-launch landing page and its email-capture form (Web redesign). The page is reached
/// via <c>/landing</c> — the <see cref="Configuration.LandingGateMiddleware"/> funnels the whole site
/// here while <c>Site:IsLanding</c> is on. Sign-ups flow through the same <see cref="ILeadRouter"/>
/// pipeline as the other forms, with the shared antiforgery + honeypot + fill-time + rate-limit guards.
/// </summary>
public sealed class LandingController : Controller
{
    private readonly ILeadRouter _router;
    private readonly LeadOptions _options;

    /// <summary>Injects the lead router and options.</summary>
    /// <param name="router">Lead router seam.</param>
    /// <param name="options">Lead options (min fill-time, inboxes).</param>
    public LandingController(ILeadRouter router, IOptions<LeadOptions> options)
    {
        _router = router;
        _options = options.Value;
    }

    /// <summary>Renders the landing page with a fresh anti-bot render token.</summary>
    /// <param name="notified">True to show the post-submit thank-you state.</param>
    [HttpGet("/landing")]
    public IActionResult Index(bool notified = false)
    {
        ViewData["Notified"] = notified;
        return View(new LandingNotifyRequest { RenderedAtUnixMs = Now() });
    }

    /// <summary>Captures a launch-notify sign-up: validate, anti-bot, route, then redirect to the thanks state.</summary>
    /// <param name="model">The submitted email-capture form.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("/landing/notify")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(LeadController.RateLimitPolicy)]
    public async Task<IActionResult> Notify(LandingNotifyRequest model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.RenderedAtUnixMs = Now(); // fresh render token for the re-shown form
            return View(nameof(Index), model);
        }

        if (AntiBot.IsHuman(model, Now(), _options.MinFormSeconds))
        {
            // Route as a general contact so the sign-up lands in the same pipeline as other leads.
            var destination = ContactRouting.ResolveInbox(ContactReason.General, _options);
            var lead = new ContactLead(
                "Landing sign-up", model.Email, ContactReason.General,
                "Launch-notify sign-up from the pre-launch landing page.", destination);
            await _router.RouteContactAsync(lead, cancellationToken);
        }

        // Redirect either way — a dropped bot submission gets the same response, revealing nothing.
        return RedirectToAction(nameof(Index), new { notified = true });
    }

    /// <summary>Current time in Unix milliseconds (for the anti-bot render token).</summary>
    private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
