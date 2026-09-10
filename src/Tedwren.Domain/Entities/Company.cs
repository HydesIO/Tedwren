using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A company on the platform (main contractor, subcontractor, agency, …). Each company sees only its
/// own view of a person, through an <see cref="Engagement"/>; one company never sees another's data
/// about a person by default (SF-2, R15).
/// </summary>
public sealed class Company
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Company name as registered by the customer.</summary>
    public required string Name { get; set; }

    /// <summary>Free-text company type (e.g. "Main Contractor", "Subcontractor"). Left open per the PRD.</summary>
    public string? Type { get; set; }

    /// <summary>
    /// Which product this company is on (PRD §2). The typed discriminator that drives the default module
    /// bundle (SF-22), the console shape (SUB-24 vs MC-23) and the sign-in semantics (R18). Nullable so
    /// companies created before this field existed simply have no product until backfilled/edited.
    /// </summary>
    public OrgType? OrgType { get; set; }

    /// <summary>Primary trade (free text).</summary>
    public string? Trade { get; set; }

    /// <summary>Companies House (or equivalent) registration number, if supplied.</summary>
    public string? RegistrationNumber { get; set; }

    /// <summary>Registered address (free text for now).</summary>
    public string? Address { get; set; }

    /// <summary>Primary contact name.</summary>
    public string? ContactName { get; set; }

    /// <summary>Primary contact email.</summary>
    public string? ContactEmail { get; set; }

    /// <summary>Primary contact phone.</summary>
    public string? ContactPhone { get; set; }

    /// <summary>When the company record was created (UTC; displayed in UK local time per R11).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
