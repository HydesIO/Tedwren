using System.ComponentModel.DataAnnotations;
using Tedwren.Web.Leads;

namespace Tedwren.Web.Models;

/// <summary>
/// The "Book a demo" form model (Web Plan §6.9, §7). Server-validated via DataAnnotations; carries the
/// pilot-interest flag and the captured UTM source so a pack-driven booking is attributable (Plan §4.1,
/// §11). Includes the hidden anti-bot fields.
/// </summary>
public sealed class DemoRequest : IAntiBotFields
{
    /// <summary>Requester's full name.</summary>
    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Work email address.</summary>
    [Required, EmailAddress, StringLength(200)]
    [Display(Name = "Work email")]
    public string WorkEmail { get; set; } = string.Empty;

    /// <summary>Company name.</summary>
    [Required, StringLength(160)]
    public string Company { get; set; } = string.Empty;

    /// <summary>Role/job title.</summary>
    [Required, StringLength(120)]
    public string Role { get; set; } = string.Empty;

    /// <summary>Company type (e.g. Subcontractor / Main Contractor).</summary>
    [Required, StringLength(80)]
    [Display(Name = "Company type")]
    public string CompanyType { get; set; } = string.Empty;

    /// <summary>Headcount or number of sites, free text (e.g. "50 operatives", "12 sites").</summary>
    [Required, StringLength(80)]
    [Display(Name = "Headcount / sites")]
    public string HeadcountOrSites { get; set; } = string.Empty;

    /// <summary>Optional phone number.</summary>
    [Phone, StringLength(40)]
    public string? Phone { get; set; }

    /// <summary>Whether the requester is interested in a pilot (a conversion signal).</summary>
    [Display(Name = "I'm interested in a pilot")]
    public bool InterestedInPilot { get; set; }

    /// <summary>Captured UTM source (e.g. "pack"), carried from the landing query into the lead.</summary>
    public string? UtmSource { get; set; }

    /// <summary>Captured UTM medium.</summary>
    public string? UtmMedium { get; set; }

    /// <summary>Captured UTM campaign.</summary>
    public string? UtmCampaign { get; set; }

    /// <inheritdoc />
    public string? Website { get; set; }

    /// <inheritdoc />
    public long RenderedAtUnixMs { get; set; }
}
