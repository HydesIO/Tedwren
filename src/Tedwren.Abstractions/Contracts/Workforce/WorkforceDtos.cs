using Tedwren.Abstractions.Common;

namespace Tedwren.Abstractions.Contracts.Workforce;

/// <summary>
/// An operative row for the org-wide Workforce register. One row per active engagement (a person engaged by
/// a company, SF-2). Compliance and next expiry are derived from the person's current cards (SF-8), never
/// invented.
/// </summary>
public sealed record OperativeListItemDto(
    Guid PersonId,
    Guid EngagementId,
    string Slug,
    string Name,
    string? Trade,
    string Company,
    ComplianceState State,
    string StatusLabel,
    DateOnly? NextExpiry);

/// <summary>
/// The full operative profile for the detail page. Fields are limited to what the domain holds: the person
/// is identified by mobile only (SF-1) and the name/trade are recorded per company on the engagement, so
/// there is no date-of-birth, NI number, email or primary site to show.
/// </summary>
public sealed record OperativeDetailDto(
    Guid PersonId,
    Guid EngagementId,
    Guid CompanyId,
    string Slug,
    string Name,
    string? Trade,
    string Company,
    string? Phone,
    ComplianceState State,
    string StatusLabel,
    IReadOnlyList<OperativeQualificationDto> Qualifications,
    IReadOnlyList<OperativeHistoryDto> History,
    // The accreditations this operative's trade requires but they do not currently hold, or hold only on an
    // expired card (SF-11 / Gate 3). Drives the "still needed" prompt on the operative's cards screen; empty
    // when nothing is outstanding or the trade has no mapped requirements.
    IReadOnlyList<string> MissingQualifications,
    // Induction is a site-entry condition for a main contractor (MC-8) but not a subcontractor (SUB-11), so it
    // is surfaced only when it applies. The reported State/StatusLabel already fold this in so "Compliant" never
    // contradicts the site gate (UAT-014); these expose the induction status on its own for the detail view.
    bool InductionApplies = false,
    bool InductionValid = false,
    string InductionStatusLabel = "",
    // Emergency contact captured for the person (MC-2), shown on the operative overview (UAT-010); null until captured.
    string? EmergencyContactName = null,
    string? EmergencyContactPhone = null);

/// <summary>A qualification card held by an operative, with its server-computed status (SF-8).</summary>
public sealed record OperativeQualificationDto(
    string Name,
    string? Issuer,
    DateOnly? ObtainedOn,
    DateOnly? ExpiresOn,
    ComplianceState State,
    string StatusLabel,
    // The captured photo of the card (SF-5) so the evidence can be viewed from the qualifications tab (UAT-010);
    // null when no image was captured.
    string? ImageReference = null);

/// <summary>A recent event in an operative's history (currently site-entry decisions, R10).</summary>
public sealed record OperativeHistoryDto(
    DateTimeOffset OccurredUtc,
    string Title,
    string? Detail);
