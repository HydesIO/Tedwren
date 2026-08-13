using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Tedwren.Web.Leads;
using Tedwren.Web.Attribution;
using Tedwren.Web.Models;

namespace Tedwren.Web.Controllers;

/// <summary>
/// Serves and processes the two lead-capture forms (Web Plan §6.9, §7). Both are server-validated with
/// antiforgery, a honeypot, a minimum fill-time and rate limiting; a genuine submission is routed via
/// <see cref="ILeadRouter"/> and the visitor is redirected to a thank-you page (post/redirect/get).
/// </summary>
public sealed class LeadController : Controller
{
    /// <summary>Rate-limit policy name applied to the form POST endpoints.</summary>
    public const string RateLimitPolicy = "leadForms";

    private readonly ILeadRouter _router;
    private readonly LeadOptions _options;

    /// <summary>Injects the lead router and options.</summary>
    /// <param name="router">Lead router seam.</param>
    /// <param name="options">Lead options.</param>
    public LeadController(ILeadRouter router, IOptions<LeadOptions> options)
    {
        _router = router;
        _options = options.Value;
    }

    /// <summary>Book a demo / pilot form at <c>/demo</c>, capturing any UTM attribution from the query.</summary>
    [HttpGet("/demo")]
    public IActionResult Demo()
    {
        var utm = Utm.FromQuery(Request.Query);
        var model = new DemoRequest
        {
            InterestedInPilot = Request.Query["pilot"] == "true",
            UtmSource = utm.Source,
            UtmMedium = utm.Medium,
            UtmCampaign = utm.Campaign,
            RenderedAtUnixMs = Now(),
        };
        return View(model);
    }

    /// <summary>Processes a demo request: validate, anti-bot, route, then redirect to thanks.</summary>
    [HttpPost("/demo")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(RateLimitPolicy)]
    public async Task<IActionResult> Demo(DemoRequest model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.RenderedAtUnixMs = Now(); // fresh render token for the re-shown form
            return View(model);
        }

        if (AntiBot.IsHuman(model, Now(), _options.MinFormSeconds))
        {
            var lead = new DemoLead(
                model.Name, model.WorkEmail, model.Company, model.Role, model.CompanyType,
                model.HeadcountOrSites, model.Phone, model.InterestedInPilot,
                model.UtmSource, model.UtmMedium, model.UtmCampaign,
                _options.SalesInbox ?? "unrouted");
            await _router.RouteDemoAsync(lead, cancellationToken);
        }

        // Redirect either way — a dropped bot submission gets the same response, revealing nothing.
        return RedirectToAction(nameof(DemoThanks), new { pilot = model.InterestedInPilot });
    }

    /// <summary>Demo thank-you page with the real booking link (Web Plan §6.9).</summary>
    [HttpGet("/demo/thanks")]
    public IActionResult DemoThanks(bool pilot)
    {
        ViewData["Pilot"] = pilot;
        return View(new DemoThanksModel(_options.BookingCalendarUrl));
    }

    /// <summary>Contact form at <c>/contact</c>.</summary>
    [HttpGet("/contact")]
    public IActionResult Contact() => View(new ContactRequest { RenderedAtUnixMs = Now() });

    /// <summary>Processes a contact message: validate, anti-bot, route by reason, then redirect to thanks.</summary>
    [HttpPost("/contact")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(RateLimitPolicy)]
    public async Task<IActionResult> Contact(ContactRequest model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.RenderedAtUnixMs = Now();
            return View(model);
        }

        if (AntiBot.IsHuman(model, Now(), _options.MinFormSeconds))
        {
            var destination = ContactRouting.ResolveInbox(model.Reason, _options);
            var lead = new ContactLead(model.Name, model.Email, model.Reason, model.Message, destination);
            await _router.RouteContactAsync(lead, cancellationToken);
        }

        return RedirectToAction(nameof(ContactThanks));
    }

    /// <summary>Contact thank-you page.</summary>
    [HttpGet("/contact/thanks")]
    public IActionResult ContactThanks() => View();

    /// <summary>Current time in Unix milliseconds (for the anti-bot render token).</summary>
    private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

/// <summary>View model for the demo thank-you page.</summary>
/// <param name="BookingUrl">The calendar booking URL, or null when not configured.</param>
public sealed record DemoThanksModel(string? BookingUrl);
