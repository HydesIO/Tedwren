using System.ComponentModel.DataAnnotations;
using Tedwren.Web.Leads;

namespace Tedwren.Web.Models;

/// <summary>
/// The partner-application form model (Web Plan §6.10, §7). Server-validated; the relationship and
/// site-access questions exist to catch the §7.3 conflict before approval. Submitting only creates a
/// pending record — never an active partner. Includes the shared anti-bot fields.
/// </summary>
public sealed class PartnerApplicationRequest : IAntiBotFields
{
    /// <summary>Applicant's name.</summary>
    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Applicant's company.</summary>
    [Required, StringLength(160)]
    public string Company { get; set; } = string.Empty;

    /// <summary>Contact email.</summary>
    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    /// <summary>How they work with subcontractors.</summary>
    [Required, StringLength(1000, MinimumLength = 10)]
    [Display(Name = "How do you work with subcontractors?")]
    public string HowTheyWork { get; set; } = string.Empty;

    /// <summary>Their relationship to any site or contractor (the §7.3 catch).</summary>
    [Required, StringLength(1000, MinimumLength = 5)]
    [Display(Name = "What's your relationship to any site or contractor?")]
    public string RelationshipToSites { get; set; } = string.Empty;

    /// <summary>Whether they control or influence access to a site (§7.3 exclusion trigger).</summary>
    [Display(Name = "I control or influence access to a site (e.g. gate, inductions, who gets on site)")]
    public bool ControlsSiteAccess { get; set; }

    /// <inheritdoc />
    public string? Website { get; set; }

    /// <inheritdoc />
    public long RenderedAtUnixMs { get; set; }
}
