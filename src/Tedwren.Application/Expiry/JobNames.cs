namespace Tedwren.Application.Expiry;

/// <summary>Canonical names for the scheduled jobs, so run records and the heartbeat check agree.</summary>
public static class JobNames
{
    /// <summary>The daily expiry-warning scan (SF-9).</summary>
    public const string ExpiryScan = "expiry-scan";

    /// <summary>The weekly expiry digest (SUB-5).</summary>
    public const string WeeklyDigest = "weekly-digest";

    /// <summary>The overnight still-signed-in check (SF-19).</summary>
    public const string OvernightCheck = "overnight-check";

    /// <summary>The recurring-form due/reminder scan (PRD-Phase 2 checklist scheduling, R12).</summary>
    public const string FormReminder = "form-reminder";
}
