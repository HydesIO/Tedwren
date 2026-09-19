namespace Tedwren.Abstractions.Contracts.Safety;

/// <summary>An accident / incident record for the list or detail view (PRD §8.2). Enum values are strings on the wire.</summary>
public sealed record IncidentReportDto(
    Guid Id,
    string Reference,
    string Kind,
    string Description,
    string? Location,
    DateTimeOffset OccurredUtc,
    string? InjuredPersonName,
    string? InjuryDetail,
    string Severity,
    string? ImmediateCause,
    string? RootCause,
    string? CorrectiveActions,
    string Status,
    bool RiddorReportable,
    string? RiddorCategory,
    string ReportedBy,
    DateTimeOffset ReportedUtc,
    string? InvestigatedBy,
    DateTimeOffset? ClosedUtc);

/// <summary>Request to record an accident / incident. <see cref="Kind"/>/<see cref="Severity"/> are enum names.</summary>
public sealed record ReportIncidentRequest(
    string Kind,
    string Description,
    string? Location,
    DateTimeOffset? OccurredUtc,
    string? InjuredPersonName,
    string? InjuryDetail,
    string? Severity);

/// <summary>Request to record/update the investigation of an incident (causes, actions and the RIDDOR flag).</summary>
public sealed record UpdateIncidentInvestigationRequest(
    string? ImmediateCause,
    string? RootCause,
    string? CorrectiveActions,
    bool RiddorReportable,
    string? RiddorCategory,
    string? InvestigatedBy);
