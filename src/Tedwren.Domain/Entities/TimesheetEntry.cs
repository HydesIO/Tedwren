namespace Tedwren.Domain.Entities;

/// <summary>
/// One line of a timesheet: the hours an operative worked on a given day at a given site, rolled up from the
/// attendance log (SUB-7). Lines are <b>append-only</b> — a correction is a new line referencing the one it
/// supersedes, carrying who changed it, when and why, so the original is retained and the change is auditable
/// (R4, R16). The effective hours for a (day, site) are those of the most recently created line in that group.
/// </summary>
public sealed class TimesheetEntry
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The timesheet this line belongs to.</summary>
    public required Guid TimesheetId { get; init; }

    /// <summary>The day worked.</summary>
    public required DateOnly WorkDate { get; init; }

    /// <summary>The site worked at.</summary>
    public required Guid SiteId { get; init; }

    /// <summary>The site name, captured for display and export.</summary>
    public required string SiteName { get; init; }

    /// <summary>The hours worked (decimal hours).</summary>
    public required decimal Hours { get; init; }

    /// <summary>The line this one corrects, if it is a correction (R16); null for an original roll-up line.</summary>
    public Guid? CorrectsEntryId { get; init; }

    /// <summary>Who made the correction; null for an original roll-up line.</summary>
    public string? Author { get; init; }

    /// <summary>Why the correction was made; null for an original roll-up line.</summary>
    public string? Reason { get; init; }

    /// <summary>When the line was created (UTC). Later lines supersede earlier ones for the same day and site.</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Whether this line is a correction of an earlier one (R16).</summary>
    public bool IsCorrection => CorrectsEntryId.HasValue;
}
