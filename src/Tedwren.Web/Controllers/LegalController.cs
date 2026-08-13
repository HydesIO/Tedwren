using Microsoft.AspNetCore.Mvc;
using Tedwren.Abstractions.Contracts.WebContent;
using Tedwren.Abstractions.Services;

namespace Tedwren.Web.Controllers;

/// <summary>
/// Serves the four legal pages (Web Plan §4) under <c>/legal/{slug}</c> from the content layer (W5).
/// The slug is constrained to the known set so an unknown legal path 404s rather than rendering an
/// empty page.
/// </summary>
public sealed class LegalController : Controller
{
    private readonly IContentProvider _content;

    /// <summary>Injects the content layer.</summary>
    /// <param name="content">Content provider.</param>
    public LegalController(IContentProvider content) => _content = content;

    /// <summary>Legal page at <c>/legal/{slug}</c> for a known document slug.</summary>
    /// <param name="slug">One of privacy, cookies, terms, data-protection.</param>
    [HttpGet("/legal/{slug:regex(^(privacy|cookies|terms|data-protection)$)}")]
    public IActionResult Page(string slug)
    {
        var document = _content.FindLegal(slug);
        if (document is null)
        {
            return NotFound();
        }

        ViewData["Title"] = document.Title;
        ViewData["MetaDescription"] = document.MetaDescription;
        return View(document);
    }
}
