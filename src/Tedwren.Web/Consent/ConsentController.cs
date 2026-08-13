using Microsoft.AspNetCore.Mvc;

namespace Tedwren.Web.Consent;

/// <summary>
/// Records a visitor's cookie-consent choice (Web Plan §5.5, §8). Works without JavaScript: the banner
/// is a form that posts here, the choice is stored in a cookie, and the visitor is returned to the page
/// they were on. Reject is a single click with the same weight as accept.
/// </summary>
public sealed class ConsentController : Controller
{
    /// <summary>Records a consent choice and returns to the originating page.</summary>
    /// <param name="choice">"all" to accept everything, anything else to reject non-essential.</param>
    /// <param name="returnUrl">Local URL to return to; falls back to home.</param>
    [HttpPost("/consent")]
    [ValidateAntiForgeryToken]
    public IActionResult Set(string? choice, string? returnUrl)
    {
        var state = choice == "all"
            ? new ConsentState(true, true, true)
            : new ConsentState(false, false, true); // reject non-essential

        Response.Cookies.Append(ConsentState.CookieName, state.ToCookieValue(), new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            IsEssential = true, // the consent record itself is a strictly-necessary cookie
            Expires = DateTimeOffset.UtcNow.AddMonths(6),
        });

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }
}
