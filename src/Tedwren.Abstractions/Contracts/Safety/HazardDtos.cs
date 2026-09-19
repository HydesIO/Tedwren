namespace Tedwren.Abstractions.Contracts.Safety;

/// <summary>A hazard / near-miss report for the list or detail view (PRD §8.2). Enum values are strings on the wire.</summary>
public sealed record HazardReportDto(
    Guid Id,
    string Reference,
    string Kind,
    string Description,
    string? Location,
    double? Latitude,
    double? Longitude,
    bool HasPhoto,
    string Severity,
    string? Category,
    string Status,
    string? AssignedTo,
    string ReportedBy,
    DateTimeOffset ReportedUtc,
    DateTimeOffset? ClosedUtc,
    string? ClosureNote);

/// <summary>Request to report a hazard / near-miss. <see cref="Kind"/>/<see cref="Severity"/> are enum names.</summary>
public sealed record ReportHazardRequest(
    string Kind,
    string Description,
    string? Location,
    double? Latitude,
    double? Longitude,
    string? PhotoBase64,
    string? PhotoContentType,
    string? Severity,
    string? Category);

/// <summary>Request to assign a hazard to a responsible person (with an optional category).</summary>
public sealed record AssignHazardRequest(string AssignedTo, string? Category);

/// <summary>Request to close a hazard out, with the required closure note (PRD §8.2).</summary>
public sealed record CloseHazardRequest(string Note);

/// <summary>Leading-indicator counts for the hazard/near-miss register (PRD §8.2 — leading-indicator statistics).</summary>
public sealed record HazardStatsDto(
    int Total,
    int Open,
    int Assigned,
    int Closed,
    int NearMiss,
    int Hazard,
    int UnsafeAct,
    int UnsafeCondition,
    int High);
