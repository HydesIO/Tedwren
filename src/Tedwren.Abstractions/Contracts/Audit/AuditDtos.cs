namespace Tedwren.Abstractions.Contracts.Audit;

/// <summary>An audit-trail entry (SF-20).</summary>
public sealed record AuditEntryDto(
    Guid Id,
    DateTimeOffset OccurredUtc,
    string Actor,
    string Action,
    string Entity,
    string? Reference,
    string? Category);

/// <summary>A request to record an audit entry.</summary>
public sealed record RecordAuditRequest(
    Guid CompanyId,
    string Actor,
    string Action,
    string Entity,
    string? Reference,
    string? Category);
