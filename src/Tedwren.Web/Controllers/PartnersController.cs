using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Tedwren.Web.Leads;
using Tedwren.Web.Models;
using Tedwren.Web.Partners;

namespace Tedwren.Web.Controllers;

/// <summary>
/// Serves the partner-programme page and processes applications (Web Plan §6.10, §7). Submitting only
/// ever creates a pending record — activation (referral code + dashboard) requires a separate human
/// approval step, so there is no self-serve activation. The dashboard is reached by a partner's unique
/// referral code, not a public listing.
/// </summary>
public sealed class PartnersController : Controller
{
    private readonly PartnerService _partners;
    private readonly LeadOptions _leadOptions;

    /// <summary>Injects the partner service and lead options (for anti-bot timing).</summary>
    /// <param name="partners">Partner service.</param>
    /// <param name="leadOptions">Lead options (shared anti-bot minimum fill time).</param>
    public PartnersController(PartnerService partners, IOptions<LeadOptions> leadOptions)
    {
        _partners = partners;
        _leadOptions = leadOptions.Value;
    }

    /// <summary>Partners / affiliates page + application form at <c>/partners</c>.</summary>
    [HttpGet("/partners")]
    public IActionResult Index() => View(new PartnerApplicationRequest { RenderedAtUnixMs = Now() });

    /// <summary>Processes a partner application: validate, anti-bot, record as pending, redirect to thanks.</summary>
    [HttpPost("/partners")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(LeadController.RateLimitPolicy)]
    public IActionResult Index(PartnerApplicationRequest model)
    {
        if (!ModelState.IsValid)
        {
            model.RenderedAtUnixMs = Now();
            return View(model);
        }

        if (AntiBot.IsHuman(model, Now(), _leadOptions.MinFormSeconds))
        {
            // Always a pending record — never an active partner (Web Plan §7.2). Approval is a human step.
            _partners.Submit(
                model.Name, model.Company, model.Email, model.HowTheyWork,
                model.RelationshipToSites, model.ControlsSiteAccess);
        }

        return RedirectToAction(nameof(Thanks));
    }

    /// <summary>Application thank-you page.</summary>
    [HttpGet("/partners/thanks")]
    public IActionResult Thanks() => View();

    /// <summary>
    /// A partner's private dashboard at <c>/partners/dashboard/{code}</c> (Web Plan §7.2). Reached by the
    /// partner's unique referral code; an unknown or inactive code 404s. Not a public listing.
    /// </summary>
    /// <param name="code">The partner's referral code.</param>
    [HttpGet("/partners/dashboard/{code}")]
    public IActionResult Dashboard(string code)
    {
        var dashboard = _partners.GetDashboard(code);
        return dashboard is null ? NotFound() : View(dashboard);
    }

    /// <summary>Current time in Unix milliseconds (for the anti-bot render token).</summary>
    private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
