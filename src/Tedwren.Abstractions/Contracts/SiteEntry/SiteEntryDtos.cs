namespace Tedwren.Abstractions.Contracts.SiteEntry;

/// <summary>One check within a site-entry decision as returned to the console (R10). Outcome is "Passed", "Failed" or "NotRun".</summary>
public sealed record DecisionCheckResultDto(string Name, string Outcome, string? Detail);

/// <summary>A day-only manager override of a blocked decision, with a reason (MC-11).</summary>
public sealed record ManagerOverrideDto(string By, string Reason);

/// <summary>A request to decide whether a worker may enter a site (MC-8).</summary>
public sealed record DecideEntryRequest(
    Guid CompanyId,
    Guid SiteId,
    Guid PersonId,
    Guid? PropertyId,
    ManagerOverrideDto? Override);

/// <summary>
/// The outcome of a site-entry decision (MC-8, MC-9, R10, R14): whether admitted, the actionable block reason,
/// every check (including those not run and why), the recorded decision id, whether a manager overrode it, and
/// the elapsed time against the &lt;3s budget (R14).
/// </summary>
public sealed record EntryDecisionResultDto(
    bool Admitted,
    string? BlockReason,
    bool WasOverridden,
    Guid DecisionId,
    long ElapsedMs,
    IReadOnlyList<DecisionCheckResultDto> Checks);

/// <summary>A worker currently present on a site, resolved to their property on a dispersed scheme (MC-12).</summary>
public sealed record MusterPersonDto(Guid PersonId, string Name, Guid? PropertyId, string? PropertyName, DateTimeOffset SinceUtc);

/// <summary>The cover status of a monitored competency on a site (MC-13). Uncovered or last-holder-present is an alert.</summary>
public sealed record CompetencyCoverDto(string Competency, bool Covered, int HoldersOnSite);

/// <summary>
/// The live muster / on-site view for a site (MC-12, MC-13, MC-14). Carries the moment it was generated so the
/// device can show the data's age when working offline (MC-14).
/// </summary>
public sealed record MusterDto(
    Guid SiteId,
    DateTimeOffset GeneratedUtc,
    IReadOnlyList<MusterPersonDto> People,
    IReadOnlyList<CompetencyCoverDto> Competencies);
