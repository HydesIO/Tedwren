namespace Tedwren.Domain.Enums;

/// <summary>
/// The state of an operative's induction attempt (MC-1–MC-7). An induction is <b>Superseded</b> when the
/// operative re-inducts (MC-7); the vocabulary never uses "denied" (R18).
/// </summary>
public enum InductionStatus
{
    /// <summary>Started but not yet passed — steps and/or the quiz remain (MC-4).</summary>
    InProgress = 0,

    /// <summary>The quiz was failed; the attempt is recorded and can be retried or reset (MC-6).</summary>
    Failed = 1,

    /// <summary>Completed: required steps done, quiz passed, signed (MC-5).</summary>
    Passed = 2,

    /// <summary>Replaced by a newer induction for the same person and template (MC-7).</summary>
    Superseded = 3,
}
