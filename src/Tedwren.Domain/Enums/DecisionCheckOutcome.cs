namespace Tedwren.Domain.Enums;

/// <summary>
/// The outcome of one check within a site-entry decision (R10). A decision must be reconstructable later,
/// including which checks ran, what each returned, and which did not run.
/// </summary>
public enum DecisionCheckOutcome
{
    /// <summary>The check ran and passed.</summary>
    Passed = 0,

    /// <summary>The check ran and failed.</summary>
    Failed = 1,

    /// <summary>The check did not run (e.g. the module that performs it is not held); the reason is recorded.</summary>
    NotRun = 2,
}
