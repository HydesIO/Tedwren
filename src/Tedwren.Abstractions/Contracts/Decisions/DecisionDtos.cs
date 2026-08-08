namespace Tedwren.Abstractions.Contracts.Decisions;

/// <summary>One check within a site-entry decision (R10). <see cref="Outcome"/> is "Passed", "Failed" or "NotRun".</summary>
public sealed record DecisionCheckDto(string Name, string Outcome, string? Detail);

/// <summary>A recorded site-entry decision (R10), reconstructable from its checks.</summary>
public sealed record SiteEntryDecisionDto(
    Guid Id,
    Guid PersonId,
    Guid SiteId,
    bool Admitted,
    DateTimeOffset OccurredUtc,
    IReadOnlyList<DecisionCheckDto> Checks);

/// <summary>A request to record a site-entry decision (written by the Phase 17 gate).</summary>
public sealed record RecordDecisionRequest(
    Guid PersonId,
    Guid SiteId,
    bool Admitted,
    IReadOnlyList<DecisionCheckDto> Checks);
