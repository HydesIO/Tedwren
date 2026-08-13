using Microsoft.AspNetCore.Http;

namespace Tedwren.Web.Consent;

/// <summary>Reads the consent choice from the request cookie (Web Plan §8).</summary>
public static class ConsentReader
{
    /// <summary>Returns the visitor's current consent state from their cookies.</summary>
    /// <param name="context">The current HTTP context.</param>
    public static ConsentState FromRequest(HttpContext context) =>
        ConsentState.Parse(context.Request.Cookies.TryGetValue(ConsentState.CookieName, out var value) ? value : null);
}
