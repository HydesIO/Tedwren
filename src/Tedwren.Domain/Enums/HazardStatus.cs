namespace Tedwren.Domain.Enums;

/// <summary>The triage state of a reported hazard / near-miss (PRD §8.2): report → assign → close.</summary>
public enum HazardStatus
{
    /// <summary>Reported and awaiting triage.</summary>
    Open = 0,

    /// <summary>Assigned to a responsible person to action.</summary>
    Assigned = 1,

    /// <summary>Closed out (with a closure note).</summary>
    Closed = 2,
}
