namespace Tedwren.Domain.Enums;

/// <summary>
/// The lifecycle state of a timesheet (SUB-8). The vocabulary is deliberately neutral — a timesheet is
/// <see cref="Returned"/> for correction, never "denied" or "rejected" (R18, SUB-12).
/// </summary>
public enum TimesheetStatus
{
    /// <summary>Accruing hours; not yet submitted for approval.</summary>
    Draft = 0,

    /// <summary>Submitted by the operative/subcontractor and awaiting a decision (SUB-27).</summary>
    Submitted = 1,

    /// <summary>Approved for payment/valuation (SUB-9).</summary>
    Approved = 2,

    /// <summary>Returned to the operative for correction, with a reason (SUB-12, R18).</summary>
    Returned = 3,
}
