namespace Tedwren.Abstractions.Common;

/// <summary>
/// Provider-neutral timesheet lifecycle state carried on DTOs (SUB-8), mirroring the domain enum without the
/// contracts depending on the domain. A timesheet is <see cref="Returned"/> for correction, never "denied"
/// (R18, SUB-12).
/// </summary>
public enum TimesheetState
{
    /// <summary>Accruing hours; not yet submitted.</summary>
    Draft = 0,

    /// <summary>Submitted and awaiting a decision.</summary>
    Submitted = 1,

    /// <summary>Approved for payment/valuation.</summary>
    Approved = 2,

    /// <summary>Returned for correction.</summary>
    Returned = 3,
}

/// <summary>
/// Provider-neutral approval granularity carried on DTOs (SUB-9), mirroring the domain enum.
/// </summary>
public enum ApprovalScope
{
    /// <summary>A single timesheet line.</summary>
    Line = 0,

    /// <summary>A whole site.</summary>
    Site = 1,

    /// <summary>A whole project.</summary>
    Project = 2,

    /// <summary>The whole period.</summary>
    All = 3,
}
