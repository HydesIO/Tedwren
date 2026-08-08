using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// The admission rule for a site-entry decision (MC-8, R2). Admission is <b>fail-closed</b>: a worker is
/// admitted only when no check failed. A check that legitimately did not run (e.g. a module the customer does
/// not hold) does not block; a check that failed — including one that errored and was recorded as failed —
/// does. Kept pure so the rule is deterministic and unit-testable.
/// </summary>
public static class SiteEntryPolicy
{
    /// <summary>Whether the checks admit the worker: true only if no check failed (R2).</summary>
    public static bool IsAdmitted(IEnumerable<DecisionCheck> checks) =>
        checks.All(c => c.Outcome != DecisionCheckOutcome.Failed);

    /// <summary>The actionable block reason from the failed checks, or null when admitted (MC-9).</summary>
    public static string? BlockReason(IEnumerable<DecisionCheck> checks)
    {
        var failed = checks.Where(c => c.Outcome == DecisionCheckOutcome.Failed).ToList();
        return failed.Count == 0
            ? null
            : string.Join("; ", failed.Select(c => c.Detail ?? c.Name));
    }
}
