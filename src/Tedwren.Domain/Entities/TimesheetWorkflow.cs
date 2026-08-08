using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// The allowed timesheet lifecycle transitions (SUB-8). Kept pure and separate from the entity so the rules
/// can be tested in isolation and reused by the service. A returned timesheet can be corrected and
/// resubmitted; an approved one is terminal for the period.
/// </summary>
public static class TimesheetWorkflow
{
    /// <summary>A draft or returned timesheet can be submitted for approval (SUB-27).</summary>
    public static bool CanSubmit(TimesheetStatus status) =>
        status is TimesheetStatus.Draft or TimesheetStatus.Returned;

    /// <summary>Only a submitted timesheet can be approved (SUB-9).</summary>
    public static bool CanApprove(TimesheetStatus status) => status is TimesheetStatus.Submitted;

    /// <summary>Only a submitted timesheet can be returned for correction (SUB-12).</summary>
    public static bool CanReturn(TimesheetStatus status) => status is TimesheetStatus.Submitted;

    /// <summary>A correction can be added while the timesheet is not yet approved (append-only, R16).</summary>
    public static bool CanCorrect(TimesheetStatus status) => status is not TimesheetStatus.Approved;
}
