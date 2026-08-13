namespace Tedwren.Domain.Enums;

/// <summary>
/// How often an assigned form is expected to be completed (the PRD-Phase 2 checklist scheduling). Ad-hoc means
/// "on demand, no cadence"; the recurring cadences drive the reminder job's due-date reminders (R12).
/// </summary>
public enum FormSchedule
{
    /// <summary>Completed on demand, with no fixed cadence.</summary>
    AdHoc = 0,

    /// <summary>Expected daily (e.g. a daily site diary).</summary>
    Daily = 1,

    /// <summary>Expected weekly.</summary>
    Weekly = 2,

    /// <summary>Expected monthly.</summary>
    Monthly = 3,
}
