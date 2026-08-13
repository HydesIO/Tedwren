using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tedwren.Web.Partners;

namespace Tedwren.Web.Controllers;

/// <summary>
/// Captures a referral code from a link (Web Plan §7): <c>/r/{code}</c> stores the code in a cookie so
/// attribution persists across sessions, then sends the visitor to the home page. The code is only
/// stored when it belongs to an active partner, so bogus codes aren't retained.
/// </summary>
public sealed class ReferralController : Controller
{
    private readonly IPartnerStore _store;
    private readonly PartnerOptions _options;

    /// <summary>Injects the partner store and options.</summary>
    /// <param name="store">Partner store.</param>
    /// <param name="options">Partner options (cookie name).</param>
    public ReferralController(IPartnerStore store, IOptions<PartnerOptions> options)
    {
        _store = store;
        _options = options.Value;
    }

    /// <summary>Stores a valid referral code in the attribution cookie and returns to home.</summary>
    /// <param name="code">The referral code from the link.</param>
    [HttpGet("/r/{code}")]
    public IActionResult Capture(string code)
    {
        var partner = _store.GetPartnerByCode(code);
        if (partner is { Active: true })
        {
            Response.Cookies.Append(_options.ReferralCookieName, partner.ReferralCode, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                IsEssential = false, // attribution is not strictly necessary; honours consent posture
                Expires = DateTimeOffset.UtcNow.AddMonths(6),
            });
        }

        return LocalRedirect("/");
    }
}
