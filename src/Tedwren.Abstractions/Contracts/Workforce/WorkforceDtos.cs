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
    string Slug,
    string Name,
    string? Trade,
    string Company,
    string? Phone,
    ComplianceState State,
    string StatusLabel,
    IReadOnlyList<OperativeQualificationDto> Qualifications,
    IReadOnlyList<OperativeHistoryDto> History);

/// <summary>A qualification card held by an operative, with its server-computed status (SF-8).</summary>
public sealed record OperativeQualificationDto(
    string Name,
    string? Issuer,
    DateOnly? ObtainedOn,
    DateOnly? ExpiresOn,
    ComplianceState State,
    string StatusLabel);

/// <summary>A recent event in an operative's history (currently site-entry decisions, R10).</summary>
public sealed record OperativeHistoryDto(
    DateTimeOffset OccurredUtc,
    string Title,
    string? Detail);
