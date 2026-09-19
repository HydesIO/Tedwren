namespace Tedwren.Mobile.Core.Forms;

/// <summary>Whether an assigned form needs completing (M6 "forms due" inbox).</summary>
public enum FormDueState
{
    /// <summary>Already completed for the current period.</summary>
    NotDue,

    /// <summary>Due in the current period (not yet completed).</summary>
    Due,

    /// <summary>Never completed, or a whole prior period was missed.</summary>
    Overdue,
}

/// <summary>
/// Computes whether an assigned form is due/overdue from its <c>FormSchedule</c> name and the operative's own
/// last-submitted time (M6). One-device-per-operative (M2) makes the device's local submission history authoritative,
/// so this needs no server round-trip. All period maths is UTC; a <see cref="TimeProvider"/> makes it testable.
/// </summary>
public static class FormsDueCalculator
{
    /// <summary>Evaluates the due state for a schedule given when the operative last submitted this form.</summary>
    public static FormDueState Evaluate(string schedule, DateTimeOffset? lastSubmittedUtc, DateTimeOffset now)
    {
        var periodStart = CurrentPeriodStart(schedule, now);
        if (periodStart is null)
        {
            // AdHoc (or unknown cadence): due until first completed, then no longer due.
            return lastSubmittedUtc is null ? FormDueState.Due : FormDueState.NotDue;
        }

        if (lastSubmittedUtc is { } last && last >= periodStart.Value)
        {
            return FormDueState.NotDue; // already completed this period
        }

        // Not completed this period. Overdue when never completed, or the last completion predates the previous period.
        var previousStart = PreviousPeriodStart(schedule, now);
        return lastSubmittedUtc is null || (previousStart is { } prev && lastSubmittedUtc.Value < prev)
            ? FormDueState.Overdue
            : FormDueState.Due;
    }

    private static DateTimeOffset? CurrentPeriodStart(string schedule, DateTimeOffset now) => schedule switch
    {
        "Daily" => StartOfDay(now),
        "Weekly" => StartOfWeek(now),
        "Monthly" => StartOfMonth(now),
        _ => null, // AdHoc / unknown
    };

    private static DateTimeOffset? PreviousPeriodStart(string schedule, DateTimeOffset now) => schedule switch
    {
        "Daily" => StartOfDay(now).AddDays(-1),
        "Weekly" => StartOfWeek(now).AddDays(-7),
        "Monthly" => StartOfMonth(now).AddMonths(-1),
        _ => null,
    };

    private static DateTimeOffset StartOfDay(DateTimeOffset now) => new(now.UtcDateTime.Date, TimeSpan.Zero);

    private static DateTimeOffset StartOfWeek(DateTimeOffset now)
    {
        var day = now.UtcDateTime.Date;
        var daysSinceMonday = ((int)day.DayOfWeek + 6) % 7;
        return new DateTimeOffset(day.AddDays(-daysSinceMonday), TimeSpan.Zero);
    }

    private static DateTimeOffset StartOfMonth(DateTimeOffset now) =>
        new(new DateTime(now.UtcDateTime.Year, now.UtcDateTime.Month, 1), TimeSpan.Zero);
}
