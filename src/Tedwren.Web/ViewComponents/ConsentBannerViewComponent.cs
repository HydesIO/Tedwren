using Microsoft.AspNetCore.Mvc;
using Tedwren.Web.Consent;

namespace Tedwren.Web.ViewComponents;

/// <summary>
/// Renders the cookie-consent banner (Web Plan §5.5, §8) — but only until the visitor has made a choice.
/// The banner is a plain form posting to <c>/consent</c>, so it works with JavaScript off, and offers
/// a one-click "Reject non-essential" with the same weight as "Accept all". No analytics/ad script is
/// emitted anywhere until consent is recorded (enforced in the layout).
/// </summary>
public sealed class ConsentBannerViewComponent : ViewComponent
{
    /// <summary>Renders the banner when no consent choice exists yet; otherwise nothing.</summary>
    public IViewComponentResult Invoke()
    {
        var state = ConsentReader.FromRequest(HttpContext);
        if (state.HasChoice)
        {
            return Content(string.Empty);
        }

        var returnUrl = HttpContext.Request.Path + HttpContext.Request.QueryString;
        return View(new ConsentBannerModel(returnUrl));
    }
}

/// <summary>View model for the consent banner.</summary>
/// <param name="ReturnUrl">The current URL to return to after recording a choice.</param>
public sealed record ConsentBannerModel(string ReturnUrl);
