namespace Tedwren.Domain.Enums;

/// <summary>
/// The granularity at which a timesheet approval is recorded (SUB-9): a single line, a whole site, a whole
/// project, or everything in the valuation period. The approver chooses the scope; it is recorded on the
/// decision so an approval can always be traced back to what was actually reviewed.
/// </summary>
public enum ApprovalScope
{
    /// <summary>A single timesheet line (one day at one site).</summary>
    Line = 0,

    /// <summary>All of an operative's lines at one site.</summary>
    Site = 1,

    /// <summary>All of an operative's lines across a project.</summary>
    Project = 2,

    /// <summary>The operative's whole timesheet for the period.</summary>
    All = 3,
}
