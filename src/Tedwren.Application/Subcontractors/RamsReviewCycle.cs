namespace Tedwren.Application.Subcontractors;

/// <summary>
/// Computes when a subcontractor's live RAMS is next due for re-review, from the configured review cycle
/// (Subcontractor Onboarding spec §4). This is beyond PRD v6.4 (§8.2 RAMS review is per-submission) and drives
/// reminders only — it never expires an approval. Pure and side-effect-free so it can be unit-tested and reused
/// by both the due-list query and the reminder engine.
/// </summary>
public static class RamsReviewCycle
{
    /// <summary>
    /// The next re-review due time: the live RAMS's last-approved time plus the cycle months. Null when there is
    /// no cycle configured, no positive cycle, or no recorded approval time on the live version.
    /// </summary>
    public static DateTimeOffset? DueUtc(int? reviewCycleMonths, DateTimeOffset? liveApprovedUtc)
    {
        if (reviewCycleMonths is not { } months || months <= 0 || liveApprovedUtc is not { } approved)
        {
            return null;
        }

        return approved.AddMonths(months);
    }

    /// <summary>Whether a re-review is due as of <paramref name="asOf"/> (false when no due date can be computed).</summary>
    public static bool IsDue(int? reviewCycleMonths, DateTimeOffset? liveApprovedUtc, DateTimeOffset asOf) =>
        DueUtc(reviewCycleMonths, liveApprovedUtc) is { } due && asOf >= due;
}
