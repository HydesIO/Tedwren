using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A timesheet is a first-class, stateful, approvable object — not a transient view over attendance rows
/// (SUB-7). It gathers an operative's hours for one week under the company that engages them (SF-2), carries
/// an approval lifecycle (SUB-8/SUB-9), and is the single object a subcontractor submits (SUB-27) and a main
/// contractor values over a period (MC-24). Unlike the append-only attendance log, the header's workflow
/// fields change as it moves through its lifecycle; its <see cref="TimesheetEntry"/> lines, however, are
/// append-only — a correction is a new line, never an edit (R16).
/// </summary>
public sealed class Timesheet
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The company the operative is engaged by (SF-2), which owns and values this timesheet (R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>The operative.</summary>
    public required Guid PersonId { get; init; }

    /// <summary>The operative's name as recorded by this company (names live on the engagement, not the person).</summary>
    public required string OperativeName { get; init; }

    /// <summary>The Monday the timesheet week starts on.</summary>
    public required DateOnly WeekStart { get; init; }

    /// <summary>Where the timesheet is in its lifecycle (SUB-8).</summary>
    public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;

    /// <summary>When the operative submitted it for approval (SUB-27).</summary>
    public DateTimeOffset? SubmittedUtc { get; set; }

    /// <summary>Who made the approval/return decision.</summary>
    public string? DecidedBy { get; set; }

    /// <summary>When the decision was made.</summary>
    public DateTimeOffset? DecidedUtc { get; set; }

    /// <summary>The granularity the approval was made at (SUB-9).</summary>
    public ApprovalScope? DecisionScope { get; set; }

    /// <summary>Why the timesheet was returned for correction, in words the operative can act on (SUB-12).</summary>
    public string? ReturnReason { get; set; }

    /// <summary>When the timesheet was created (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
