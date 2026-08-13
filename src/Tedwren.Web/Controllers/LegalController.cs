using Microsoft.AspNetCore.Mvc;

namespace Tedwren.Web.Controllers;

/// <summary>
/// Serves the four legal pages (Web Plan §4) under <c>/legal/{slug}</c>. The slug is constrained to
/// the known set so an unknown legal path 404s rather than rendering an empty page. Real legal copy
/// lands as content in W5; W1 renders a titled stub.
/// </summary>
public sealed class LegalController : Controller
{
    /// <summary>The known legal document slugs and their display titles.</summary>
    private static readonly IReadOnlyDictionary<string, string> Documents = new Dictionary<string, string>
    {
        ["privacy"] = "Privacy Policy",
        ["cookies"] = "Cookie Policy",
        ["terms"] = "Terms of Service",
        ["data-protection"] = "Data Protection",
    };

    /// <summary>Legal page at <c>/legal/{slug}</c> for a known document slug.</summary>
    /// <param name="slug">One of privacy, cookies, terms, data-protection.</param>
    [HttpGet("/legal/{slug:regex(^(privacy|cookies|terms|data-protection)$)}")]
    public IActionResult Page(string slug)
    {
        ViewData["Title"] = Documents[slug];
        return View();
    }
}
